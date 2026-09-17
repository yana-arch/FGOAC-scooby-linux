"""Explicit, finite account upgrades using the installed game's master data."""
from collections import defaultdict
from copy import deepcopy
from datetime import datetime, timezone
from pathlib import Path

ACTIONS = ('servants', 'craft-essences', 'clear-gifts', 'quests', 'bond', 'costumes', 'materials', 'levels')


def apply_action(servlet, profile, action, catalog):
    if action not in ACTIONS:
        raise ValueError('Unknown account action')
    now = datetime.now(timezone.utc).isoformat(timespec='seconds')
    count = 0
    if action in ('servants', 'craft-essences'):
        kind = 1 if action == 'servants' else 2
        owned = servlet._confirmed_summon_card_counts(profile)
        cards = [card for card in servlet._trading_card_master_by_id().values()
                 if card['TrcTypeId'] == kind
                 for _ in range(max(0, 5 - int(owned.get(str(card['TradingCardId']), 0))))]
        if cards:
            servlet._acquire_summon_ticket_cards(profile, cards)
        count = len(cards)
    elif action == 'clear-gifts':
        servlet._materialize_free_summon_ticket_inventory(profile)
        gifts = profile.get('unused_present_box_list', [])
        history = profile.setdefault('removed_present_box_list', [])
        for gift in gifts:
            history.append({**deepcopy(gift), 'removed_time': now})
            issue = str(gift.get('_summon_ticket_issue', ''))
            ticket = profile.get('summon_ticket_entitlements', {}).get(issue)
            if isinstance(ticket, dict):
                ticket['amount'] = max(0, int(ticket.get('amount', 0)) - 1)
        count = len(gifts)
        profile['unused_present_box_list'] = []
    elif action == 'materials':
        for item in catalog:
            if not item['key'].startswith('material:'):
                continue
            _, category, item_id = item['key'].split(':')
            delta = max(0, item['max_amount'] - item['current'])
            servlet._change_material_amount(profile, int(category), int(item_id), delta)
            count += int(delta > 0)
    elif action == 'costumes':
        rights = profile.setdefault('spiritual_costume_rights', {})
        for setting in servlet._spiritual_costume_settings()['sp_cos_info_list']:
            key = str(setting['costume_id'])
            if int(rights.get(key, 0)) < 1:
                rights[key] = 1
                count += 1
    elif action == 'quests':
        state = profile.setdefault('quest_progress_by_singularity', {})
        for chapter in servlet._load_local_quest_ids():
            existing = {r['quest_id']: r for r in state.get(str(chapter), [])}
            for base in servlet._initial_quest_progress(chapter):
                row = existing.setdefault(base['quest_id'], base)
                count += int(not servlet._quest_progress_row_is_cleared(row))
                row.update(best_result=max(0, row.get('best_result', -1)),
                           is_mission_all_clear=True, quest_clear_for_mission=max(1, row.get('quest_clear_for_mission', 0)),
                           time_for_sync=now)
            state[str(chapter)] = list(existing.values())
        coop = {r['coop_quest_id']: r for r in profile.get('coop_quest_progress_list', [])}
        for quest in servlet._active_coop_quest_ids():
            row = coop.setdefault(quest, {'coop_quest_id': quest})
            row['sortie_count'] = max(1, row.get('sortie_count', 0))
            row['clear_count'] = max(1, row.get('clear_count', 0))
        profile['coop_quest_progress_list'] = list(coop.values())
        # A finite admin ledger, separate from earned battle evidence and rewards.
        completed = profile.setdefault('local_admin_completed_achievements', {})
        for setting in servlet._achievement_settings():
            completed[str(setting['id'])] = max(1, setting['nrm'])
        servlet._seed_story_quest_boss_progress(profile)
    else:
        inventory = servlet._build_player_servant_inventory(profile)
        root = Path(servlet.app_root).parent / 'Server/data/fgo-master'
        def rows(folder, table):
            return servlet._load_property_rows(str(root / folder / ('arms_mst_' + table + '.bin')), table)
        limits = rows('svt', 'svt_limit') if action == 'levels' else []
        skill_rows = rows('skill', 'svt_skill') if action == 'levels' else []
        support_rows = rows('skill', 'svt_support_skill') if action == 'levels' else []
        np_rows = rows('np', 'svt_noble_phantasm') if action == 'levels' else []

        limits_by_sid = defaultdict(list)
        for r in limits:
            if 0 <= int(r['limit_count']) <= 4:
                limits_by_sid[int(r['svt_id'])].append(int(r['limit_count']))

        skills_by_sid_num = defaultdict(lambda: defaultdict(list))
        for r in skill_rows:
            skills_by_sid_num[int(r['svt_id'])][int(r['num'])].append(int(r['priority']))

        support_by_sid_num = defaultdict(lambda: defaultdict(list))
        for r in support_rows:
            support_by_sid_num[int(r['svt_id'])][int(r['num'])].append(int(r['priority']))

        np_by_sid = defaultdict(list)
        for r in np_rows:
            np_by_sid[int(r['svt_id'])].append(int(r['priority']))

        for row in inventory:
            before = deepcopy(row)
            sid = int(row['svt_id'])
            # This returns the native terminal level's lower EXP bound.
            cap = servlet._battle_bond_exp_cap(sid, int(row.get('bond_exceed_count', 0)))
            base_level, bounds = servlet._battle_bond_exp_rules_cache.get(sid, (0, {}))
            if bounds:
                row['bond_exceed_count'] = max(row.get('bond_exceed_count', 0), max(bounds) - base_level)
                cap = servlet._battle_bond_exp_cap(sid, row['bond_exceed_count'])
            row['bnd_exp'] = max(row.get('bnd_exp', 0), cap)
            if action == 'levels':
                stages = limits_by_sid.get(sid, [])
                if not stages:
                    raise ValueError(f'Missing level master for servant {sid}')
                row['lmt_cnt'] = max(stages)
                if row.get('is_enable_exceed', 1) and row['lmt_cnt'] == 4:
                    while True:
                        rule = servlet._servant_exceed_rule(sid, int(row.get('exceed_count', 0)))
                        if rule is None:
                            break
                        row['exceed_count'] = rule['destination_exceed_count']
                exp = servlet._servant_synthesis_exp_cap(row)
                if exp is None:
                    raise ValueError(f'Missing EXP master for servant {sid}')
                row['exp'] = max(row.get('exp', 0), exp)
                row['skl_lv'] = [10, 10, 10]
                row['sprt_skl_lv'] = [10, 10, 10]
                row['max_np_lv'] = 5
                row['skl_stp'] = [max(skills_by_sid_num[sid].get(slot, []), default=1) for slot in (1, 2, 3)]
                row['sprt_skl_stp'] = [max(support_by_sid_num[sid].get(slot, []), default=1) for slot in (1, 2, 3)]
                row['np_stp'] = max(np_by_sid.get(sid, []), default=row.get('np_stp', 0))
            profile.setdefault('svt_state', {})[str(sid)] = row
            count += int(row != before)
        servlet._merge_support_skills_into_inventory(inventory)
    profile.setdefault('local_admin_actions', {})[action] = {'at': now, 'changed': count}
    return count
