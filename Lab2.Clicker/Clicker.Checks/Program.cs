using System.Globalization;
using Clicker.Core;

int passed = 0;

var examples = new (string Input, string Expected)[]
{
    ("0", "0"),
    ("000000", "0"),
    ("7", "7"),
    ("999", "999"),
    ("1000", "1000"),
    ("1001", "1001"),
    ("1000000", "1000000"),
    ("1005017", "1005017"),
    ("19005017", "19005017"),
    ("000001005017", "1005017"),
    ("9223372036854775808", "9223372036854775808"),
    (new string('9', 1000), new string('9', 1000)),
    ("1" + new string('0', 999) + "1", "1" + new string('0', 999) + "1")
};

foreach (var example in examples)
{
    var number = new BigNumber(example.Input);
    ExpectText(number.ToString(), example.Expected);
}

ExpectException<ArgumentNullException>(() => new BigNumber(null!));
foreach (string invalid in new[] { "", "-1", "+1", "1.2", "1,2", " 1", "1 ", "1 000", "abc", "١٢٣" })
    ExpectException<FormatException>(() => new BigNumber(invalid));

// Строковое представление не должно зависеть от региональных настроек.
CultureInfo originalCulture = CultureInfo.CurrentCulture;
try
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
    ExpectText(new BigNumber("1005017").ToString(), "1005017");
}
finally
{
    CultureInfo.CurrentCulture = originalCulture;
}

Console.WriteLine($"Passed: {passed} checks.");

void ExpectText(string actual, string expected)
{
    if (actual != expected)
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");

    passed++;
}

void ExpectException<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        passed++;
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
