using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace gym_api.Services
{
    public class NewebPayOptions
    {
        public string MerchantID { get; set; } = "";
        public string HashKey { get; set; } = "";
        public string HashIV { get; set; } = "";
        public string Version { get; set; } = "1.6";

        // 測試環境
        public string GatewayUrl { get; set; } = "https://ccore.newebpay.com/MPG/mpg_gateway";
    }

    public class NewebPayCreatePayload
    {
        public string GatewayUrl { get; set; } = "";
        public string MerchantID { get; set; } = "";
        public string TradeInfo { get; set; } = "";
        public string TradeSha { get; set; } = "";
        public string Version { get; set; } = "";
    }

    public class NewebPayService
    {
        private readonly NewebPayOptions _opt;

        public NewebPayService(NewebPayOptions opt)
        {
            _opt = opt;
        }

        /// <summary>
        /// 產生送往藍新 MPG 的 MerchantID/TradeInfo/TradeSha/Version
        /// </summary>
        public NewebPayCreatePayload CreateMpgPayload(
            string merchantOrderNo,
            int amt,
            string itemDesc,
            string returnUrl,
            string notifyUrl,
            string email = "",
            string? customerUrl = null,
            bool credit = true)
        {
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var kv = new List<KeyValuePair<string, string?>>
            {
                // ⚠️ 這個 MerchantID 是 TradeInfo 內的欄位（文件就是這樣）
                new("MerchantID", _opt.MerchantID),
                new("RespondType", "JSON"),
                new("TimeStamp", timeStamp),
                new("Version", _opt.Version),
                new("MerchantOrderNo", merchantOrderNo),
                new("Amt", amt.ToString()),
                new("ItemDesc", itemDesc),
                new("ReturnURL", returnUrl),
                new("NotifyURL", notifyUrl),
            };

            if (!string.IsNullOrWhiteSpace(email))
                kv.Add(new("Email", email));

            if (!string.IsNullOrWhiteSpace(customerUrl))
                kv.Add(new("CustomerURL", customerUrl));

            if (credit)
                kv.Add(new("CREDIT", "1"));

            var tradeInfoPlain = BuildQueryString(kv);

            var tradeInfoHex = EncryptToHex(tradeInfoPlain, _opt.HashKey, _opt.HashIV);

            var tradeSha = CreateTradeSha(tradeInfoHex, _opt.HashKey, _opt.HashIV);

            return new NewebPayCreatePayload
            {
                GatewayUrl = _opt.GatewayUrl,
                MerchantID = _opt.MerchantID,
                TradeInfo = tradeInfoHex,
                TradeSha = tradeSha,
                Version = _opt.Version
            };
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string?>> kvs)
        {
            return string.Join("&",
                kvs.Where(kv => !string.IsNullOrEmpty(kv.Value))
                   .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));
        }
        private static string EncryptToHex(string plainText, string hashKey, string hashIv)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var padded = AddPkcs7Padding(plainBytes, 32);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = Encoding.UTF8.GetBytes(hashKey);  
            aes.IV = Encoding.UTF8.GetBytes(hashIv);

            using var enc = aes.CreateEncryptor();
            var cipherBytes = enc.TransformFinalBlock(padded, 0, padded.Length);

            return Convert.ToHexString(cipherBytes).ToLowerInvariant();
        }
        public static string CreateTradeSha(string tradeInfoHex, string hashKey, string hashIv)
        {
            var raw = $"HashKey={hashKey}&{tradeInfoHex}&HashIV={hashIv}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToUpperInvariant();
        }

        private static byte[] AddPkcs7Padding(byte[] data, int blockSize)
        {
            var pad = blockSize - (data.Length % blockSize);
            if (pad == 0) pad = blockSize;

            var output = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, output, 0, data.Length);

            for (int i = data.Length; i < output.Length; i++)
                output[i] = (byte)pad;

            return output;
        }
    }
}