using System.Collections.Generic;
using System.Threading.Tasks;
using CodeAnalyzer.Roslyn.Models;
using VectorStore.Core;
using Microsoft.Extensions.Logging;

/// <summary>
/// Verbosity levels for controlling output detail
/// </summary>
public enum VerbosityLevel
{
    Terse,    // Minimal output
    Normal,   // Standard output
    Verbose   // Detailed output including debug information
}

namespace CodeAnalyzer.Roslyn.Tests
{
    /// <summary>
    /// Adapter that wraps the actual FileVectorStore to implement IVectorStoreWriter.
    /// This provides real integration with the VectorStore package.
    /// </summary>
    public class FileVectorStoreAdapter : IVectorStoreWriter
    {
        private readonly FileVectorStore _store;
        private readonly ILogger<FileVectorStore> _logger;

        public FileVectorStoreAdapter(FileVectorStore store, ILogger<FileVectorStore>? logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? new NullLogger<FileVectorStore>();
        }

        public async Task<string> AddTextAsync(string content, Dictionary<string, object> metadata)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be null or empty", nameof(content));

            try
            {
                // Use the real FileVectorStore.AddTextAsync method
                var result = await _store.AddTextAsync(content, metadata, null).ConfigureAwait(false);
                return result; // result is already a string (document ID)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add text to vector store");
                throw;
            }
        }

        /// <summary>
        /// Creates a new FileVectorStoreAdapter with a real FileVectorStore instance.
        /// </summary>
        /// <param name="storePath">Path for the vector store</param>
        /// <param name="verbosity">Verbosity level for logging</param>
        /// <returns>Adapter with a new vector store</returns>
        public static async Task<FileVectorStoreAdapter> CreateAsync(string storePath, VerbosityLevel verbosity = VerbosityLevel.Normal)
        {
            ILogger<FileVectorStore> logger = verbosity switch
            {
                VerbosityLevel.Terse => new SilentLogger<FileVectorStore>(),
                VerbosityLevel.Normal => new SuppressedWarningLogger<FileVectorStore>(),
                VerbosityLevel.Verbose => new VerboseLogger<FileVectorStore>(),
                _ => new SuppressedWarningLogger<FileVectorStore>()
            };
            
            var options = new VectorStoreOptions
            {
                StorePath = storePath,
                ChunkSize = 1000,
                EnableEmbeddingGeneration = true,
                UseMemoryMapping = true
            };
            
            // Create or open the vector store
            var store = await FileVectorStore.CreateOrOpenAsync(storePath, options, logger).ConfigureAwait(false);
            
            return new FileVectorStoreAdapter(store, logger);
        }

        /// <summary>
        /// Gets the underlying vector store for testing purposes.
        /// </summary>
        public FileVectorStore Store => _store;

        /// <summary>
        /// Disposes the underlying vector store.
        /// </summary>
        public void Dispose()
        {
            _store?.Dispose();
        }
    }

    /// <summary>
    /// Simple null logger implementation for testing.
    /// </summary>
    public class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    /// <summary>
    /// Logger that suppresses warnings about config files and other harmless messages.
    /// </summary>
    public class SuppressedWarningLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Only log errors and above, suppress warnings about config files
            if (logLevel >= LogLevel.Error)
            {
                var message = formatter(state, exception);
                Console.WriteLine($"[{logLevel}] {message}");
            }
        }
    }

    /// <summary>
    /// Logger that suppresses all output (for terse mode).
    /// </summary>
    public class SilentLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    /// <summary>
    /// Logger that shows all output including debug messages (for verbose mode).
    /// </summary>
    public class VerboseLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        
        public bool IsEnabled(LogLevel logLevel) => true;
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Console.WriteLine($"[{logLevel}] {message}");
        }
    }
}
