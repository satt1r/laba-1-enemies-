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
    CheckComparison(leftText, rightText, leftValue.CompareTo(rightValue));

    if (leftValue >= rightValue)
        ExpectText((left - right).ToString(), (leftValue - rightValue).ToString(CultureInfo.InvariantCulture));
    else
        ExpectException<InvalidOperationException>(() => { _ = left - right; });

    ExpectText(left.ToString(), leftText);
    ExpectText(right.ToString(), rightText);
}

var one = new BigNumber("1");
var zero = new BigNumber("0");
Expect(!ReferenceEquals(one + zero, one), "Addition must return a new object.");
Expect(!ReferenceEquals(one - zero, one), "Subtraction must return a new object.");
ExpectText((one + one).ToString(), "2");
ExpectText((one - one).ToString(), "0");
ExpectText(one.ToString(), "1");
ExpectException<InvalidOperationException>(() => { _ = zero - one; });
ExpectException<ArgumentNullException>(() => { _ = one + null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! + one; });
ExpectException<ArgumentNullException>(() => { _ = one - null!; });
ExpectException<ArgumentNullException>(() => { _ = (BigNumber)null! - one; });
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

Console.WriteLine($"Passed: {passed} checks.");

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
