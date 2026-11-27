using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace WaterSurfaceControls
{
    internal enum PresetFileState { Unavailable, Empty, Ready, Invalid }

    internal sealed class PresetFiles
    {
        private readonly string folder;
        private readonly PresetFileState[] states = new PresetFileState[3];
        private readonly string[] messages = new string[3];
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        internal PresetFiles(string folder)
        {
            this.folder = folder;
        }

        private static bool Valid(PresetSlot slot) => slot >= PresetSlot.One && slot <= PresetSlot.Three;
        private static int Index(PresetSlot slot) => (int)slot - 1;
        private string PathFor(PresetSlot slot) => Path.Combine(folder, "Preset-" + (int)slot + ".ini");
        internal PresetFileState State(PresetSlot slot) => Valid(slot) ? states[Index(slot)] : PresetFileState.Unavailable;
        internal string FileInfoFor(PresetSlot slot) => Valid(slot) ? messages[Index(slot)] ?? PathFor(slot) : folder;

        internal void Refresh()
        {
            for (int i = 1; i <= states.Length; ++i)
                Read((PresetSlot)i);
        }

        internal WaterPreset Read(PresetSlot slot)
        {
            if (!Valid(slot))
                return null;
            int i = Index(slot);
            string path = PathFor(slot);
            messages[i] = null;
            try
            {
                var preset = ReadFile(path);
                states[i] = PresetFileState.Ready;
                return preset;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                states[i] = PresetFileState.Empty;
            }
            catch (Exception ex) when (FileError(ex))
            {
                states[i] = ex is InvalidDataException || ex is DecoderFallbackException
                    ? PresetFileState.Invalid : PresetFileState.Unavailable;
                Report(slot, ex);
            }
            return null;
        }

        private static WaterPreset ReadFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                if (stream.Length > 65536)
                    throw new InvalidDataException("Preset exceeds 64 KiB.");
                using (var reader = new StreamReader(stream, Utf8, true))
                {
                    var lines = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    return WaterPreset.FromFile(lines) ?? throw new InvalidDataException("Invalid or unsupported Water Surface Controls preset.");
                }
            }
        }

        internal void Write(PresetSlot slot, WaterPreset preset, bool replace)
        {
            if (!Valid(slot))
                return;
            try
            {
                Commit(PathFor(slot), preset, replace);
                states[Index(slot)] = PresetFileState.Ready;
                messages[Index(slot)] = null;
            }
            catch (Exception ex) when (FileError(ex))
            {
                Read(slot);
                Report(slot, ex);
            }
        }

        private void Commit(string destination, WaterPreset preset, bool replace)
        {
            Directory.CreateDirectory(folder);
            string temporary = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Utf8))
                {
                    foreach (string line in preset.ToFileLines())
                        writer.WriteLine(line);
                }
                // Both files are in the same directory. Only an explicit replacement may overwrite.
                if (replace)
                    File.Replace(temporary, destination, null);
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        // Retire an embedded slot only after a matching copy exists on disk.
        // A conflicting external slot stays selected; its legacy copy gets a separate filename.
        internal bool Migrate(PresetSlot slot, string embedded)
        {
            if (!Valid(slot) || string.IsNullOrEmpty(embedded))
                return false;
            var preset = WaterPreset.FromLegacy(embedded);
            if (preset == null)
            {
                Mod.Logger.Warn("Could not migrate the embedded preset in slot " + (int)slot + ". Kept in preferences.");
                return false;
            }
            try
            {
                string path = PathFor(slot);
                if (!InstallIfMissingOrIdentical(path, preset))
                {
                    string backup = Path.Combine(folder, "Preset-" + (int)slot + "-v1.5.0.ini");
                    if (!InstallIfMissingOrIdentical(backup, preset))
                        throw new IOException("Conflicting migration backup: " + backup);
                    Mod.Logger.Info("Saved legacy preset to " + backup);
                }
                Read(slot);
                return true;
            }
            catch (Exception ex) when (FileError(ex))
            {
                Report(slot, ex);
                return false;
            }
        }

        private bool InstallIfMissingOrIdentical(string path, WaterPreset preset)
        {
            try
            {
                return ReadFile(path).ToFileLines().SequenceEqual(preset.ToFileLines());
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Commit(path, preset, false);
                return true;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is DecoderFallbackException)
            {
                return false;
            }
        }

        internal void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true })?.Dispose();
            }
            catch (Exception ex) when (FileError(ex) || ex is Win32Exception || ex is InvalidOperationException)
            {
                Mod.Logger.Warn("Cannot open preset folder " + folder + ": " + ex.Message);
            }
        }

        private static bool FileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException
            || ex is SecurityException || ex is InvalidDataException || ex is DecoderFallbackException || ex is NotSupportedException;

        private void Report(PresetSlot slot, Exception ex)
        {
            string message = PathFor(slot) + " — " + ex.Message;
            messages[Index(slot)] = message;
            Mod.Logger.Warn(message);
        }
    }
}
