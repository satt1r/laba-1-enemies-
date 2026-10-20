using System.Globalization;
using Clicker.Core;
using Clicker.Checks;

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

var sums = new (string Left, string Right, string Expected)[]
{
    ("0", "0", "0"),
    ("999", "1", "1000"),
    ("1", "999999", "1000000"),
    ("3712117", "15512900", "19225017"),
    ("1000000000", "1001", "1000001001"),
    (new string('9', 1200), "1", "1" + new string('0', 1200))
};

foreach (var example in sums)
{
    var left = new BigNumber(example.Left);
    var right = new BigNumber(example.Right);
    ExpectText((left + right).ToString(), example.Expected);
    ExpectText(left.ToString(), example.Left);
    ExpectText(right.ToString(), example.Right);
}

var differences = new (string Left, string Right, string Expected)[]
{
    ("0", "0", "0"),
    ("1000", "999", "1"),
    ("1000000", "1", "999999"),
    ("3712117", "1911900", "1800217"),
    ("999999", "999", "999000"),
    ("1000000", "1000000", "0"),
    ("1" + new string('0', 1200), "1", new string('9', 1200))
};

foreach (var example in differences)
{
    var left = new BigNumber(example.Left);
    var right = new BigNumber(example.Right);
    ExpectText((left - right).ToString(), example.Expected);
    ExpectText(left.ToString(), example.Left);
    ExpectText(right.ToString(), example.Right);
}

string largeNines = new string('9', 600);
string largeSquare = new string('9', 599) + "8" + new string('0', 599) + "1";
var products = new (string Left, string Right, string Expected)[]
{
    ("0", "999999999", "0"),
    ("1", "1000001", "1000001"),
    ("999", "999", "998001"),
    ("7135654", "23", "164120042"),
    ("1001", "1001", "1002001"),
    ("999999", "999999", "999998000001"),
    ("2147483647", "2147483647", "4611686014132420609"),
    (largeNines, largeNines, largeSquare),
    (new string('9', 1200), "1000", new string('9', 1200) + "000")
};

foreach (var example in products)
{
    var left = new BigNumber(example.Left);
    var right = new BigNumber(example.Right);
    ExpectText((left * right).ToString(), example.Expected);
    ExpectText((right * left).ToString(), example.Expected);
    ExpectText(left.ToString(), example.Left);
    ExpectText(right.ToString(), example.Right);
}

var quotients = new (string Left, string Right, string Expected)[]
{
    ("0", "123", "0"),
    ("1", "2", "0"),
    ("10", "3", "3"),
    ("10014217", "3", "3338072"),
    ("1000000", "1000", "1000"),
    ("998001", "999", "999"),
    ("1002001", "1001", "1001"),
    ("1000001", "1001", "999"),
    ("999998000001", "999999", "999999"),
    ("2147483647", "2147483647", "1"),
    (new string('9', 1200), "1", new string('9', 1200)),
    ("1" + new string('0', 1200), "1000", "1" + new string('0', 1197)),
    (largeSquare, largeNines, largeNines),
    ("1", largeNines, "0")
};

foreach (var example in quotients)
{
    var left = new BigNumber(example.Left);
    var right = new BigNumber(example.Right);
    ExpectText((left / right).ToString(), example.Expected);
    ExpectText(left.ToString(), example.Left);
    ExpectText(right.ToString(), example.Right);
}

CheckComparison("0", "0000", 0);
CheckComparison("000123", "123", 0);
CheckComparison("1000000", "999999", 1);
CheckComparison("1000001", "1001000", -1);
CheckComparison("1005017", "1005016", 1);
CheckComparison(new string('9', 1200), "1" + new string('0', 1200), -1);

// Сверяем случайные примеры с обычной арифметикой в безопасном диапазоне int.
var random = new Random(42);
for (int i = 0; i < 200; i++)
{
    int leftValue = random.Next(1_000_000_000);
    int rightValue = random.Next(1_000_000_000);
    string leftText = leftValue.ToString(CultureInfo.InvariantCulture);
    string rightText = rightValue.ToString(CultureInfo.InvariantCulture);
    var left = new BigNumber(leftText);
    var right = new BigNumber(rightText);

    ExpectText((left + right).ToString(), (leftValue + rightValue).ToString(CultureInfo.InvariantCulture));
    // long используется только здесь как независимый эталон для небольших чисел.
    string expectedProduct = ((long)leftValue * rightValue).ToString(CultureInfo.InvariantCulture);
    ExpectText((left * right).ToString(), expectedProduct);
    ExpectText((left * rightValue).ToString(), expectedProduct);
    ExpectText((leftValue * right).ToString(), expectedProduct);
    if (rightValue != 0)
    {
        string expectedQuotient = (leftValue / rightValue).ToString(CultureInfo.InvariantCulture);
        ExpectText((left / right).ToString(), expectedQuotient);
        ExpectText((left / rightValue).ToString(), expectedQuotient);
    }
    CheckComparison(leftText, rightText, leftValue.CompareTo(rightValue));

    if (leftValue >= rightValue)
        ExpectText((left - right).ToString(), (leftValue - rightValue).ToString(CultureInfo.InvariantCulture));
    else
        ExpectException<InvalidOperationException>(() => { _ = left - right; });

    ExpectText(left.ToString(), leftText);
    ExpectText(right.ToString(), rightText);
}

// Для больших делителей проверяем частное и остаток: a = b * q + r, 0 <= r < b.
for (int i = 0; i < 25; i++)
{
    var divisor = new BigNumber(RandomNumber(random, random.Next(10, 100)));
    var expectedQuotient = new BigNumber(RandomNumber(random, random.Next(10, 100)));
    var remainder = new BigNumber("7");
    BigNumber dividend = divisor * expectedQuotient + remainder;
    BigNumber quotient = dividend / divisor;
    BigNumber actualRemainder = dividend - quotient * divisor;
    Expect(quotient == expectedQuotient, "Large quotient is incorrect.");
    Expect(actualRemainder == remainder, "Large remainder is incorrect.");
    Expect(actualRemainder < divisor, "Remainder must be less than divisor.");
}

var one = new BigNumber("1");
var zero = new BigNumber("0");
Expect(!ReferenceEquals(one + zero, one), "Addition must return a new object.");
Expect(!ReferenceEquals(one - zero, one), "Subtraction must return a new object.");
Expect(!ReferenceEquals(one * one, one), "Multiplication must return a new object.");
Expect(!ReferenceEquals(one / one, one), "Division must return a new object.");
ExpectText((one * 0).ToString(), "0");
ExpectText((0 * one).ToString(), "0");
ExpectText((one * int.MaxValue).ToString(), "2147483647");
ExpectText((new BigNumber("4611686014132420609") / int.MaxValue).ToString(), "2147483647");
ExpectText((new BigNumber("1000") * 12 / 10).ToString(), "1200");
ExpectText((new BigNumber("1") * 12 / 10).ToString(), "1");
ExpectText((one + one).ToString(), "2");
ExpectText((one - one).ToString(), "0");
ExpectText(one.ToString(), "1");
ExpectException<InvalidOperationException>(() => { _ = zero - one; });
ExpectException<ArgumentNullException>(() => { _ = one + null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! + one; });
ExpectException<ArgumentNullException>(() => { _ = one - null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! - one; });
ExpectException<ArgumentNullException>(() => { _ = one * null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! * one; });
ExpectException<ArgumentNullException>(() => { _ = one / null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! / one; });
ExpectException<DivideByZeroException>(() => { _ = one / zero; });
ExpectException<DivideByZeroException>(() => { _ = zero / zero; });
ExpectException<DivideByZeroException>(() => { _ = one / new BigNumber("0000"); });
ExpectException<DivideByZeroException>(() => { _ = one / 0; });
ExpectException<ArgumentOutOfRangeException>(() => { _ = one * -1; });
ExpectException<ArgumentOutOfRangeException>(() => { _ = -1 * one; });
ExpectException<ArgumentOutOfRangeException>(() => { _ = one / -1; });
ExpectException<ArgumentNullException>(() => { _ = one < null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! > one; });

BigNumber? missing = null;
Expect(missing == null, "Null values must compare equal.");
Expect(one != missing, "A number must differ from null.");
Expect(!new BigNumber("1").Equals((object?)null), "A number must not equal null.");
Expect(!one.Equals((object)"1"), "A number must not equal a string.");
Expect(one.CompareTo(null) > 0, "A number must sort after null.");
Expect(one.Equals((object)new BigNumber("0001")), "Object equality must use numeric value.");
Expect(one.GetHashCode() == new BigNumber("0001").GetHashCode(), "Equal values need equal hashes.");
var uniqueNumbers = new HashSet<BigNumber> { one, new BigNumber("0001") };
Expect(uniqueNumbers.Count == 1, "HashSet must recognize equal numeric values.");
Expect(one == new BigNumber("1000") - new BigNumber("999"), "Subtraction must remove leading zero blocks.");

passed += GameChecks.Run();
Console.WriteLine($"Passed: {passed} checks.");

string RandomNumber(Random source, int length)
{
    var digits = new char[length];
    digits[0] = (char)('0' + source.Next(1, 10));
    for (int i = 1; i < digits.Length; i++)
        digits[i] = (char)('0' + source.Next(10));

    return new string(digits);
}

void CheckComparison(string leftText, string rightText, int expected)
{
    var left = new BigNumber(leftText);
    var right = new BigNumber(rightText);
    Expect(Math.Sign(left.CompareTo(right)) == Math.Sign(expected), "CompareTo disagrees with numeric order.");
    Expect((left == right) == (expected == 0), "Equality disagrees with numeric order.");
    Expect((left != right) == (expected != 0), "Inequality disagrees with numeric order.");
    Expect((left < right) == (expected < 0), "Less-than disagrees with numeric order.");
    Expect((left > right) == (expected > 0), "Greater-than disagrees with numeric order.");
    Expect((left <= right) == (expected <= 0), "Less-than-or-equal disagrees with numeric order.");
    Expect((left >= right) == (expected >= 0), "Greater-than-or-equal disagrees with numeric order.");
}

void Expect(bool condition, string description)
{
    if (!condition)
        throw new InvalidOperationException(description);

    passed++;
}

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
