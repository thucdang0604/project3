using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JamesThewProject.Helpers
{
    public static class VnpayHelper
    {
        // Hàm tính HMAC SHA512
        public static string ComputeHmacSHA512(string key, string data)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        // Hàm tạo URL thanh toán VNPay với việc URL-encode các giá trị tham số
        public static string BuildPaymentUrl(string vnpUrl, Dictionary<string, string> parameters, string hashSecret)
        {
            var sortedParams = parameters.OrderBy(p => p.Key);
            // Tạo chuỗi dữ liệu để tính checksum (không encode)
            string hashData = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}"));
            string secureHash = ComputeHmacSHA512(hashSecret, hashData);
            // Tạo chuỗi query với URL-encoded cho các giá trị tham số
            string queryString = string.Join("&", sortedParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            queryString += $"&vnp_SecureHash={secureHash}";
            return $"{vnpUrl}?{queryString}";
        }


        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
