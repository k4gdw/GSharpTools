using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DetectDuplicates.Annotations;
using GSharpTools;

namespace DetectDuplicates {
	/// <summary>
	/// Find executable files on the PATH environment 
	/// </summary>
	internal class DetectDuplicates {
		/// <summary>
		/// this is a dictionary of the following format:
		/// Cache[File Size][MD5 Hash] = Existing Filename
		/// </summary>
		private readonly Dictionary<long, Dictionary<string, string>> _cache =
			new Dictionary<long, Dictionary<string, string>>();

		private readonly CachedHashes _databaseCache = new CachedHashes();
		private readonly DateTime _startupTime = DateTime.Now;
		private InputArgs _args;

		private long _bytesChecked;
		private long _bytesDetected;
		private bool _deleteFiles;
		private long _filesChecked;
		private long _filesDetected;
		private long _hashesRead;

		/// <summary>
		/// This flag indicates whether subdirectories should be recursed into or not.
		/// </summary>
		private bool _recursive = true;

		[UsedImplicitly]
		private long _timeSpentCalculatingMd5S;

		/// <summary>
		/// Initializes a new instance of the <see cref="DetectDuplicates"/> class.
		/// </summary>
		public DetectDuplicates() {
			_timeSpentCalculatingMd5S = 0;
			_deleteFiles = false;
			_bytesChecked = 0;
			_filesChecked = 0;
			_bytesDetected = 0;
			_filesDetected = 0;
			_hashesRead = 0;
		}

		/// <summary>
		/// Calculates the MD5.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>System.String.</returns>
		private string CalculateMd5(string filename) {
			var start = DateTime.Now.Ticks;

			var result = _databaseCache.LookupHash(filename);
			if (result != null)
				return result;

			using (HashAlgorithm hashAlg = MD5.Create()) {
				try {
					using (Stream file = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
						byte[] hash = hashAlg.ComputeHash(file);

						// Display the hash code of the file to the console.
						result = BitConverter.ToString(hash);

						_databaseCache.Write(result, filename);
						++_hashesRead;
					}
				}
				catch (IOException e) {
#if DEBUG
					Console.WriteLine("Sorry, unable to calculate MD5 hash for '{0}': {1}.", filename, e.Message);
#endif
					return "";
				}
			}
			_timeSpentCalculatingMd5S += DateTime.Now.Ticks - start;
			return result;
		}

		/// <summary>
		/// This program finds an executable on the PATH. It can also find other stuff on the path, but 
		/// mostly it finds the executable.s
		/// </summary>
		/// <param name="args"></param>
		private void Run(string[] args) {
			Console.OutputEncoding = Encoding.GetEncoding(Encoding.Default.CodePage);
			_args = new InputArgs(
				"DETECTDUPLICATES",
				string.Format("Version {0}\r\nFreeware written by Gerson Kurz (http://p-and-q.com)",
					AppVersion.Get()));

			_args.Add(InputArgType.Flag, "recursive", false, Presence.Optional, "search directories recursively");
			_args.Add(InputArgType.Parameter, "cache", null, Presence.Optional, "cache file name");
			_args.Add(InputArgType.Flag, "delete", false, Presence.Optional, "delete files (default: list only)");
			_args.Add(InputArgType.RemainingParameters, "DIR {DIR}", null, Presence.Required, "one or more directories to search");

			if (_args.Process(args)) {
				_databaseCache.Initialize(_args.GetString("cache"), _cache);
				_recursive = _args.GetFlag("recursive");
				_deleteFiles = _args.GetFlag("delete");

				foreach (string directory in _args.GetStringList("DIR {DIR}")) {
					Read(SanitizeInput(directory));
				}
			}
			if (_filesChecked <= 0) return;
			_databaseCache.Flush();

			TimeSpan elapsed = DateTime.Now - _startupTime;

			Console.WriteLine("____________________________________________________________________________________");
			Console.WriteLine("DetectDuplicates finished after {0}", elapsed);

			Console.WriteLine("Checked a total of {0} files using {1}, calculating {2} hashes.",
				_filesChecked,
				Tools.BytesAsString(_bytesChecked),
				_hashesRead);

			Console.WriteLine("Of these, {0} files [{2:0.00%}] using {1} [{3:0.00%}] were duplicates.",
				_filesDetected,
				Tools.BytesAsString(_bytesDetected),
				((double) _filesDetected)/((double) _filesChecked),
				((double) _bytesDetected)/((double) _bytesChecked));
		}

		/// <summary>
		/// Reads the specified directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		private void Read(string directory) {
			if (!Directory.Exists(directory)) {
				Console.WriteLine("Warning, {0} doesn't exist...", directory);
				return;
			}

			string[] files;
			try {
				files = Directory.GetFiles(directory);
			}
			catch (Exception e) {
				Console.WriteLine("Sorry, unable to read directory '{0}': {1}.", directory, e.Message);
				return;
			}
			foreach (
				string readpath in files.Select(filename => directory.Equals(".") ? filename : Path.Combine(directory, filename))) {
				Check(readpath);
			}
			if (!_recursive) return;
			foreach (string subdir in Directory.GetDirectories(directory)) {
				Read(Path.Combine(directory, subdir));
			}
		}

		/// <summary>
		/// Indicates if a duplicate file has been found and deletes it if the /delete
		/// command line switch was used.
		/// </summary>
		/// <param name="fi">The FIleINfo opject for the file inn question.</param>
		/// <param name="filename">The filename.</param>
		/// <param name="existing">The existing.</param>
		private void Found(FileInfo fi, string filename, string existing) {
			if (filename.Equals(existing, StringComparison.OrdinalIgnoreCase)) return;
			Console.WriteLine("{0} already exists as {1} [{2} bytes]",
				filename, existing, fi.Length);

			++_filesDetected;
			_bytesDetected += fi.Length;

			if (!_deleteFiles) return;
			File.SetAttributes(filename, FileAttributes.Normal);
			File.Delete(filename);
		}

		/// <summary>
		/// Checks the specified filename.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <exception cref="System.ArgumentNullException">filename</exception>
		private void Check([NotNull] string filename) {
			if (filename == null) throw new ArgumentNullException("filename");
			FileInfo fi;
			try {
				fi = new FileInfo(filename);
			}
			catch (Exception) {
				return;
			}

			long size = fi.Length;
			++_filesChecked;
			_bytesChecked += size;

			if (_cache.ContainsKey(size)) {
				Dictionary<string, string> temp = _cache[size];

				if (temp.ContainsKey("\0")) {
					Debug.Assert(temp.Count == 1);

					string tempname = temp["\0"];

					string secondHash = CalculateMd5(tempname);
					if (secondHash == "") return;
					temp.Remove("\0");
					temp.Add(secondHash, tempname);
				}

				// this most definitely needs to be calculated

				string hash = CalculateMd5(filename);

				if (temp.ContainsKey(hash)) {
					Found(fi, filename, temp[hash]);
				}
				else {
					temp[hash] = filename;
				}
			}
			else {
				// because the file size is new, by definition cache cannot exist yet
				var temp = new Dictionary<string, string>();
				temp["\0"] = filename; // don't read yet, read only when required
				_cache[size] = temp;
			}
		}

		/// <summary>
		/// Removes the last path part.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>System.String.</returns>
		private static string RemoveLastPathPart(string text) {
			string[] tokens = text.Split('\\');
			var result = new StringBuilder();
			bool first = true;
			for (int i = 0; i < (tokens.Length - 1); ++i) {
				if (first)
					first = false;
				else
					result.Append("\\");
				result.Append(tokens[i]);
			}
			return result.ToString();
		}

		/// <summary>
		/// Sanitizes the input.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>System.String.</returns>
		private static string SanitizeInput(string input) {
			bool isUncPath = false;
			if (input.StartsWith("\\\\")) {
				isUncPath = true;
				input = input.Substring(3);
			}

			string[] tokens = input.Split('\\');

			// replace leading "." / ".." tokens             
			for (int i = 0; i < tokens.Length; ++i) {
				if (i == 0) {
					switch (tokens[0]) {
						case ".":
							tokens[0] = Directory.GetCurrentDirectory();
							break;
						case "..": {
							string currentDirectory = Directory.GetCurrentDirectory();
							DirectoryInfo parent = Directory.GetParent(currentDirectory);
							if (parent != null)
								tokens[0] = parent.FullName;
							else
								tokens[0] = currentDirectory;
						}
							break;
					}
				}
				else {
					switch (tokens[i]) {
						case "..": {
							Debug.Assert(i > 0);
							tokens[i] = null;

							int j = i - 1;
							while ((j >= 0) && (tokens[j] == null)) {
								--j;
							}
							if (j >= 0) {
								tokens[j] = tokens[j].Contains("\\") ? RemoveLastPathPart(tokens[j]) : null;
							}
							if (tokens[0] == null)
								tokens[0] = Directory.GetCurrentDirectory();
						}
							break;
						case ".":
							tokens[i] = null;
							break;
					}
				}
			}

			var result = new StringBuilder();
			bool first = true;
			if (isUncPath) {
				result.Append("\\\\");
				first = false;
			}
			foreach (string t in tokens.Where(t => t != null)) {
				if (!first)
					result.Append("\\");
				else
					first = false;

				result.Append(t);
			}
			return result.ToString();
		}

		/// <summary>
		/// Main function: defer program logic
		/// </summary>
		/// <param name="args"></param>
		private static void Main(string[] args) {
			new DetectDuplicates().Run(args);
#if DEBUG
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
#endif
		}
	}
}