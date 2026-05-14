using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

namespace ArkAI
{
    public static class DataManager
    {
        // Global değişkenler: Uygulama boyunca giriş yapan kullanıcıya buradan erişeceğiz
        public static int CurrentUserId { get; set; } = -1;
        public static string CurrentUsername { get; set; }

        // 1. Donanım ID'sini al ve Hashle (Güvenlik için)
        public static string GetVehicleUUID()
        {
            string rawId = "";
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (ManagementObject obj in searcher.Get()) { rawId = obj["UUID"].ToString(); break; }
            }
            catch { rawId = "ARK-DEV-2026"; }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // 2. Aracı Tanı ve Kullanıcıyı Otomatik Getir (Login için)
        public static bool AutoLogin()
        {
            string vuid = GetVehicleUUID();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                // vehicles tablosundaki UUID ile users tablosundaki kullanıcıyı eşleştiriyoruz
                string sql = @"SELECT u.id, u.username FROM users u 
                               INNER JOIN vehicles v ON u.id = v.id 
                               WHERE v.vehicle_uuid = @vuid LIMIT 1";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@vuid", vuid);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CurrentUserId = reader.GetInt32("id");
                        CurrentUsername = reader.GetString("username");
                        return true;
                    }
                }
            }
            return false;
        }

        // 3. Mevcut Araca Ait Tüm Detayları Çek (Profile.cs'de kullanacağız)
        public static DataTable GetCurrentVehicleInfo()
        {
            DataTable dt = new DataTable();
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM vehicles WHERE vehicle_uuid = @vuid";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@vuid", GetVehicleUUID());

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            return dt;
        }

        // 4. Yeni Aracı Mevcut Kullanıcıya Kaydet/Bağla
        public static void RegisterCurrentVehicle(int userId)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string sql = "INSERT IGNORE INTO vehicles (id, vehicle_uuid) VALUES (@uid, @vuid)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@vuid", GetVehicleUUID());
                cmd.ExecuteNonQuery();
            }
        }
    }
}