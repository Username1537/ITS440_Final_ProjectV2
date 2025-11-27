using SQLite;
using ITS440_Final_ProjectV2.Models;
using System.Diagnostics;

namespace ITS440_Final_ProjectV2.Services
{
    public class GameDatabase
    {
        private SQLiteAsyncConnection _database;

        public GameDatabase()
        {
            // Initialize the database path
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "games.db");
            _database = new SQLiteAsyncConnection(dbPath);
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await _database.CreateTableAsync<Game>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Database] Initialization error: {ex.Message}");
            }
        }

        // Add a new game
        public async Task<int> AddGameAsync(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.Title))
                throw new ArgumentException("Game title cannot be empty.");

            game.DateAdded = DateTime.Now;
            return await _database.InsertAsync(game);
        }

        // Get all games
        public async Task<List<Game>> GetAllGamesAsync()
        {
            return await _database.Table<Game>().ToListAsync();
        }

        // Get completed games
        public async Task<List<Game>> GetCompletedGamesAsync()
        {
            return await _database.Table<Game>()
                .Where(g => g.IsCompleted)
                .ToListAsync();
        }

        // Get incomplete games
        public async Task<List<Game>> GetIncompleteGamesAsync()
        {
            return await _database.Table<Game>()
                .Where(g => !g.IsCompleted)
                .ToListAsync();
        }

        // Update a game
        public async Task<int> UpdateGameAsync(Game game)
        {
            return await _database.UpdateAsync(game);
        }

        // Delete a game
        public async Task<int> DeleteGameAsync(int id)
        {
            return await _database.DeleteAsync<Game>(id);
        }

        // Delete all games
        public async Task<int> DeleteAllGamesAsync()
        {
            return await _database.DeleteAllAsync<Game>();
        }
    }
}