using System;
using System.Collections.Generic;
using System.Data.Common;
using GSharpTools;
using GSharpTools.DBTools;
using GSharpSQLite;
using System.Diagnostics;

namespace DetectDuplicates
{
	/// <summary>
	/// Class CachedHashes
	/// </summary>
    class CachedHashes
    {
        private readonly Dictionary<string, string> _cacheValues = new Dictionary<string, string>();

		/// <summary>
		/// Checks the dictionary for the hash previously cached for the specified file
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>System.String.</returns>
        public string LookupHash(string filename)
        {
            filename = filename.ToLower();
            return _cacheValues.ContainsKey(filename) ? _cacheValues[filename] : null;
        }

        /// <summary>
        /// If configured, this function reads from a SQLite database caching previously known hashes.
        /// The motivation for this is that the most expensive operation would be the MD5 hash calculation;
        /// That means that on a second run of DetectDuplicates, we may want to reuse existing known MD5 hashes.
        /// </summary>
        public bool Initialize(string databaseFilename, Dictionary<long, Dictionary<string, string>> cache)
        {
            if (string.IsNullOrEmpty(databaseFilename))
                return false;

            _filename = databaseFilename;

            try
            {
                // connect to database
                Trace.TraceInformation("About to read cache from \"{0}\"", _filename);
                _database = new Database("Data Source=" + _filename);

                // make sure lookup table exists
                _database.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS hashes (hash TEXT, filename TEXT);");

                DateTime cacheStartTime = DateTime.Now;

                // read known hashes from lookup table
                int cachedHashesRead = 0;
                using (var ss = new SelectStatement(_database, "SELECT hash, filename FROM hashes", null))
                {
                    while (ss.Next())
                    {
                        string hash = ss.AsText(0);
                        string filename = ss.AsText(1);
                        _cacheValues[filename.ToLower()] = hash;
                        ++cachedHashesRead;
                    }
                }
                if (cachedHashesRead == 0)
                {
                    Console.WriteLine("Cache is empty as of yet...");
                }
                else
                {
                    TimeSpan elapsed = DateTime.Now - cacheStartTime;
                    Console.WriteLine("Read {0} hashes from the cache in {1}...", cachedHashesRead, elapsed);
                }
                return true;
            }
            catch (Exception e)
            {
                Tools.DumpException(e, "ReadCache() caught an exception while reading \"{0}\"", _filename);
                return false;
            }
        }

		/// <summary>
		/// Flushes this instance.
		/// </summary>
        public void Flush() {
	        if (_database == null) return;
	        if (_transaction == null) return;
	        Debug.Assert(_command != null);
	        Debug.Assert(_sizeUsed > 0);

	        _transaction.Commit();
	        _command.Dispose();
	        _command = null;
	        _transaction.Dispose();
	        _transaction = null;
	        _sizeUsed = 0;
        }

		/// <summary>
		/// Writes the specified hash.
		/// </summary>
		/// <param name="hash">The hash.</param>
		/// <param name="filename">The filename.</param>
	    public void Write(string hash, string filename)
        {
	        if (_database == null) return;
	        // create transaction object if it doesn't exist yet
	        if( _transaction == null )
	        {
		        Debug.Assert(_sizeUsed == 0);
		        Debug.Assert(_command == null);

		        _transaction = _database.CreateTransaction();
		        _command = _database.CreateCommand("INSERT INTO hashes (hash, filename) VALUES (?,?)");

		        _hashTextField = _command.CreateParameter();
		        _fileNameField = _command.CreateParameter();

		        _command.Parameters.Add(_hashTextField);
		        _command.Parameters.Add(_fileNameField);
	        }

	        _hashTextField.Value = hash;
	        _fileNameField.Value = filename;
	        _command.ExecuteNonQuery();

	        ++_sizeUsed;
	        if (_sizeUsed >= FlushSize)
	        {
		        Flush();
	        }
        }

        /// <summary>
        /// Filename for database
        /// </summary>
        private string _filename;

        private DbParameter _hashTextField;
        private DbParameter _fileNameField;

        /// <summary>
        /// Connection to SQLite database
        /// </summary>
        private DBConnection _database;

        /// <summary>
        /// Transaction used to speed up the processing
        /// </summary>
        private DbTransaction _transaction;

        /// <summary>
        /// Command used during an active transaction
        /// </summary>
        private DbCommand _command;

        /// <summary>
        /// flush transaction every 10000 elements
        /// </summary> 
        private const int FlushSize = 1000;

        /// <summary>
        /// current size of elements in cache 
        /// </summary>
        private int _sizeUsed;
    }
}
