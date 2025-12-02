using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UtilitiesLibrary;

public static class Extensions
{
    public static string ToHex(this byte[] bytes)
    {
        StringBuilder sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }

    public static string MD5(this string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);

        // Convert byte array to hexadecimal string
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2")); // Lowercase hex format
        }

        return sb.ToString();
    }
}