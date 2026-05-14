using MySql.Data.MySqlClient;

namespace ArkAI
{
    public class Database
    {
        // GITHUB İÇİN GİZLENDİ: Kendi veritabanı bağlantı bilgilerinizi buraya girin
        private static string connectionString = "Server=localhost;Database=YOUR_DATABASE_NAME;Uid=YOUR_USERNAME;Pwd=YOUR_PASSWORD;";

        public static MySqlConnection GetConnection() => new MySqlConnection(connectionString);
    }
}