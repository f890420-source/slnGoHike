namespace prjGoHike.Services
{
    ///  BCrypt 取代 LoginController 原本的 SHA256。

    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool Verify(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // 資料庫若殘留損壞或非 BCrypt 密碼，不應讓登入 API 變成 500。
                return false;
            }
        }
    }
}
