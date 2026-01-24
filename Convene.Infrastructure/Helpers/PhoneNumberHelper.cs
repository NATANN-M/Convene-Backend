using System.Text.RegularExpressions;

public static class PhoneNumberHelper
{
    public static string NormalizeEthiopianPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Remove spaces, +, -, brackets, etc.
        phone = Regex.Replace(phone, @"\D", "");

        // Convert 2519xxxxxxxx ? 09xxxxxxxx
        if (phone.StartsWith("2519"))
            phone = "0" + phone.Substring(3);

        // Convert 2517xxxxxxxx ? 07xxxxxxxx
        if (phone.StartsWith("2517"))
            phone = "0" + phone.Substring(3);

        // Ensure only valid formats are saved
        if (phone.StartsWith("09") || phone.StartsWith("07"))
            return phone;

        // Invalid format
        return string.Empty;
    }
}
