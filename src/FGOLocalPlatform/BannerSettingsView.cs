using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace FGOLocalPlatform;

public partial class BannerSettingsView : UserControl
{
	private const string PatchMarker = "fgo_event_toggles_v1";

	private static readonly string[] BannerIds =
	{
		"LTE8008", "LTE8010", "LTE8011", "LTE8012", "LTE8013", "LTE8014",
		"LTE8015", "LTE8016", "LTE8017", "LTE8018", "LTE8019", "LTE8020",
		"LTE8021", "LTE8022", "LTE8023", "LTE8025", "LTE8027", "LTE8028",
		"LTE8029", "LTE8030", "LTE8031", "LTE8032", "LTE8033", "LTE8034",
		"LTE8035", "LTE8036", "LTE8038", "LTE8039", "LTE8040", "LTE8041",
		"LTE8042", "LTE8043", "LTE8044", "LTE8045", "LTE8046", "LTE8047",
		"LTE8048", "LTE8049"
	};

	private readonly Dictionary<string, CheckBox> bannerOptions = new();

	private bool ready;

	private string ServerRoot => Path.GetFullPath(Path.Combine(GamePaths.GameRoot, "..", "Server"));

	private string ServerYamlPath => Path.GetFullPath(Path.Combine(ServerRoot, "artemis", "config", "fgo.yaml"));

	public BannerSettingsView()
	{
		InitializeComponent();
		foreach (string id in BannerIds)
		{
			if (FindName(id) is CheckBox checkBox)
			{
				bannerOptions[id] = checkBox;
			}
		}
		LoadEnabledSingularityIds();
		ready = true;
	}

	private void Option_OnChanged(object sender, RoutedEventArgs e)
	{
		if (!ready)
		{
			return;
		}
		StatusText.Text = "Banner settings updated. Make sure you press Save.";
	}

	public void Save()
	{
		try
		{
			WriteEnabledSingularityIds();
			StatusText.Text = "Banner settings saved. Stop and start the local server from the Play page for the changes to take effect.";
		}
		catch (Exception ex)
		{
			StatusText.Text = "Could not save the banner settings: " + ex.Message;
		}
	}

	private void Save_OnClick(object sender, RoutedEventArgs e)
	{
		Save();
	}

	private void DisableAll_OnClick(object sender, RoutedEventArgs e)
	{
		foreach (CheckBox checkBox in bannerOptions.Values)
		{
			if (checkBox != null)
			{
				checkBox.IsChecked = false;
			}
		}
		StatusText.Text = "Banner selection cleared. Press Save to turn the filter off.";
	}

	private void ApplyEventPatch_OnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			PatchStatusText.Text = ApplyEventTogglePatch();
		}
		catch (Exception ex)
		{
			PatchStatusText.Text = ex.Message;
		}
	}

	private string ApplyEventTogglePatch()
	{
		using Stream? patchStream = typeof(BannerSettingsView).Assembly.GetManifestResourceStream("FGOLocalPlatform.EventTogglePatch.json");
		if (patchStream == null)
		{
			return "The embedded event toggle patch could not be found.";
		}
		using StreamReader patchReader = new StreamReader(patchStream);
		string patchText = patchReader.ReadToEnd();
		using JsonDocument document = JsonDocument.Parse(patchText);

		if (!document.RootElement.TryGetProperty("edits", out JsonElement editsElement)
			|| editsElement.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				"The event toggle patch file is missing the edits array.");
		}
		List<string> messages = new List<string>();
		// Every edit is checked before any file is written, so a mismatch cannot leave one patched.
		List<(string TargetPath, string UpdatedText)> pending = new List<(string, string)>();
		foreach (JsonElement editElement in editsElement.EnumerateArray())
		{
			string fileName = editElement.TryGetProperty("file", out JsonElement fileElement) ? fileElement.GetString() : null;
			string anchorOld = editElement.TryGetProperty("anchor_old", out JsonElement oldElement) ? oldElement.GetString() : null;
			string anchorNew = editElement.TryGetProperty("anchor_new", out JsonElement newElement) ? newElement.GetString() : null;
			if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(anchorOld) || string.IsNullOrEmpty(anchorNew))
			{
				throw new InvalidOperationException("One of the patch entries is missing its file, anchor_old or anchor_new value.");
			}

			string targetPath = ResolvePatchTarget(fileName);
			if (!File.Exists(targetPath))
			{
				throw new InvalidOperationException(targetPath + " was not found. The server files may have been updated since this launcher was built.");
			}

			string currentText = File.ReadAllText(targetPath);
			string fileLineEnding = currentText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
			string normalizedAnchorNew = anchorNew
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace("\n", fileLineEnding, StringComparison.Ordinal);
			bool configPropertyAlreadyApplied = string.Equals(fileName, "config.py", StringComparison.OrdinalIgnoreCase)
				&& currentText.Contains("def enabled_singularity_ids", StringComparison.Ordinal);
			if (ContainsLineEndingInsensitive(currentText, anchorNew)
				|| configPropertyAlreadyApplied
				|| currentText.Contains(PatchMarker, StringComparison.OrdinalIgnoreCase))
			{
				messages.Add(Path.GetFileName(targetPath) + " already has the patch applied.");
				continue;
			}

			MatchCollection oldMatches = FindLineEndingInsensitiveMatches(currentText, anchorOld);
			if (oldMatches.Count != 1)
			{
				throw new InvalidOperationException(targetPath + " does not match what this patch expects. The server files may have been updated since this launcher was built.");
			}

			Match oldMatch = oldMatches[0];
			pending.Add((targetPath, currentText.Substring(0, oldMatch.Index)
				+ normalizedAnchorNew
				+ currentText.Substring(oldMatch.Index + oldMatch.Length)));
		}

		foreach ((string targetPath, string updatedText) in pending)
		{
			try
			{
				// Pre-patch copy for ValidatePythonFile; AtomicFile.Write owns the plain .bak slot.
				string backupPath = targetPath + ".event-toggle.bak";
				File.Copy(targetPath, backupPath, overwrite: true);
				AtomicFile.WriteAllText(targetPath, updatedText);
				ValidatePythonFile(targetPath, backupPath);
			}
			catch (Exception ex)
			{
				// Earlier files in this pass are still patched, so the report has to name them.
				messages.Add(ex.Message);
				throw new InvalidOperationException(string.Join(Environment.NewLine, messages));
			}
			messages.Add("Applied to " + Path.GetFileName(targetPath));
		}

		return string.Join(Environment.NewLine, messages.Count > 0 ? messages : new[] { "No event-toggle changes were needed." });
	}

	private static bool ContainsLineEndingInsensitive(string text, string value)
	{
		return FindLineEndingInsensitiveMatches(text, value).Count > 0;
	}

	private static MatchCollection FindLineEndingInsensitiveMatches(string text, string value)
	{
		string[] lines = value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
		string pattern = string.Join("\\r?\\n", lines.Select(Regex.Escape));
		return Regex.Matches(text, pattern, RegexOptions.CultureInvariant);
	}

	// Ticks the boxes for the ids in fgo.yaml; any other value leaves every box clear.
	private void LoadEnabledSingularityIds()
	{
		try
		{
			if (!File.Exists(ServerYamlPath))
			{
				return;
			}
			Match entry = Regex.Match(File.ReadAllText(ServerYamlPath), @"^[ \t]*enabled_singularity_ids:[ \t]*\[([^\]]*)\]", RegexOptions.Multiline);
			foreach (Match id in Regex.Matches(entry.Groups[1].Value, "[0-9]+"))
			{
				if (bannerOptions.TryGetValue("LTE" + id.Value, out CheckBox? checkBox))
				{
					checkBox.IsChecked = true;
				}
			}
		}
		catch (Exception ex)
		{
			StatusText.Text = "Could not read the saved banner settings: " + ex.Message;
		}
	}

	private void WriteEnabledSingularityIds()
	{
		string path = ServerYamlPath;
		if (!File.Exists(path))
		{
			throw new IOException("The server config file was not found: " + path);
		}

		List<int> selectedIds = bannerOptions
			.Where(pair => pair.Value != null && pair.Value.IsChecked == true && pair.Key.StartsWith("LTE", StringComparison.Ordinal))
			.Select(pair => int.Parse(pair.Key.Substring(3), System.Globalization.CultureInfo.InvariantCulture))
			.OrderBy(id => id)
			.ToList();

		string valueText = selectedIds.Count == 0 ? "null" : "[" + string.Join(", ", selectedIds) + "]";
		string original = File.ReadAllText(path);
		string lineEnding = original.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
		bool trailingNewline = original.EndsWith("\n", StringComparison.Ordinal);
		List<string> lines = new List<string>(original.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
		if (trailingNewline)
		{
			lines.RemoveAt(lines.Count - 1);
		}
		int serverStart = lines.FindIndex(line => line.TrimEnd() == "server:");
		if (serverStart < 0)
		{
			throw new InvalidOperationException("fgo.yaml has no server: block. The file may have been edited by hand.");
		}
		// The block ends at the next top-level key; an entry is replaced with its continuation lines.
		int insertAt = serverStart + 1;
		int replaceFrom = -1;
		int replaceTo = -1;
		int keyIndent = 0;
		for (int i = serverStart + 1; i < lines.Count; i++)
		{
			string trimmed = lines[i].TrimStart();
			if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			int indent = lines[i].Length - trimmed.Length;
			if (indent == 0)
			{
				break;
			}
			if (replaceTo == i && (indent > keyIndent
				|| (indent == keyIndent && trimmed.StartsWith("- ", StringComparison.Ordinal))))
			{
				replaceTo = i + 1;
			}
			else if (trimmed.StartsWith("enabled_singularity_ids:", StringComparison.Ordinal))
			{
				keyIndent = indent;
				replaceFrom = i;
				replaceTo = i + 1;
			}
			insertAt = i + 1;
		}

		if (replaceFrom < 0)
		{
			replaceFrom = insertAt;
			replaceTo = insertAt;
		}
		lines.RemoveRange(replaceFrom, replaceTo - replaceFrom);
		lines.Insert(replaceFrom, "  enabled_singularity_ids: " + valueText);
		AtomicFile.WriteAllText(path, string.Join(lineEnding, lines) + (trailingNewline ? lineEnding : string.Empty));
	}

	private string ResolvePatchTarget(string fileName)
	{
		return Path.GetFullPath(Path.Combine(ServerRoot, "artemis", "titles", "fgo", fileName));
	}

	private static void ValidatePythonFile(string path, string backupPath)
	{
		string python = Path.GetFullPath(Path.Combine(GamePaths.GameRoot, "..", "Server", "python", "python.exe"));
		if (!File.Exists(python))
		{
			return;
		}

		ProcessStartInfo startInfo = new ProcessStartInfo(python)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardError = true,
			WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
		};
		startInfo.ArgumentList.Add("-m");
		startInfo.ArgumentList.Add("py_compile");
		startInfo.ArgumentList.Add(path);

		using Process process = Process.Start(startInfo) ?? throw new IOException("Could not run Python validation for the patched file.");
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			if (File.Exists(backupPath))
			{
				File.Copy(backupPath, path, overwrite: true);
			}
			throw new InvalidOperationException("The patched Python file failed validation and has been restored from backup. Error: " + error.Trim());
		}
	}
}
