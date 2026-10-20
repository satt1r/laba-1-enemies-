using System.Globalization;
using System.Text;

namespace Clicker.Core;

public sealed class BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    private const int BlockBase = 1000;
    private const int DigitsPerBlock = 3;

    // Младший блок находится в начале массива. Массив не передаётся наружу.
    private readonly int[] blocks;

    public BigNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
            throw new FormatException("Число не может быть пустой строкой.");

        foreach (char digit in value)
        {
            if (digit < '0' || digit > '9')
                throw new FormatException("Число должно содержать только цифры от 0 до 9.");
        }

        // У нуля остаётся одна цифра, у остальных чисел убираем ведущие нули.
        int firstDigit = 0;
        while (firstDigit < value.Length - 1 && value[firstDigit] == '0')
            firstDigit++;

        int digitCount = value.Length - firstDigit;
        blocks = new int[(digitCount - 1) / DigitsPerBlock + 1];

        int blockIndex = 0;
        int place = 1;

        for (int i = value.Length - 1; i >= firstDigit; i--)
        {
            blocks[blockIndex] += (value[i] - '0') * place;
            place *= 10;

            if (place == BlockBase)
            {
                place = 1;
                blockIndex++;
            }
        }
    }

    private BigNumber(int[] result)
    {
        int length = result.Length;
        while (length > 1 && result[length - 1] == 0)
            length--;

        // Копируем блоки, чтобы у нового числа был собственный массив.
        blocks = new int[length];
        Array.Copy(result, blocks, length);
    }

    public int CompareTo(BigNumber? other)
    {
        if (other is null)
            return 1;

        if (blocks.Length != other.blocks.Length)
            return blocks.Length.CompareTo(other.blocks.Length);

        for (int i = blocks.Length - 1; i >= 0; i--)
        {
            if (blocks[i] != other.blocks[i])
                return blocks[i].CompareTo(other.blocks[i]);
        }

        return 0;
    }

    public bool Equals(BigNumber? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is BigNumber other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int block in blocks)
            hash.Add(block);

        return hash.ToHashCode();
    }

    public static BigNumber operator +(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Add(right);
    }

    public static BigNumber operator -(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Subtract(right);
    }

    public static BigNumber operator *(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Multiply(right);
    }

    public static BigNumber operator /(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Divide(right);
    }

    public static BigNumber operator *(BigNumber left, int right) => left * FromNonNegativeInt(right);
    public static BigNumber operator *(int left, BigNumber right) => FromNonNegativeInt(left) * right;
    public static BigNumber operator /(BigNumber left, int right) => left / FromNonNegativeInt(right);

    private static BigNumber FromNonNegativeInt(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Число не может быть отрицательным.");

        return new BigNumber(value.ToString(CultureInfo.InvariantCulture));
    }

    public static bool operator ==(BigNumber? left, BigNumber? right) => object.Equals(left, right);
    public static bool operator !=(BigNumber? left, BigNumber? right) => !object.Equals(left, right);
    public static bool operator <(BigNumber left, BigNumber right) => Compare(left, right) < 0;
    public static bool operator >(BigNumber left, BigNumber right) => Compare(left, right) > 0;
    public static bool operator <=(BigNumber left, BigNumber right) => Compare(left, right) <= 0;
    public static bool operator >=(BigNumber left, BigNumber right) => Compare(left, right) >= 0;

    private static int Compare(BigNumber left, BigNumber right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right);
    }

    private BigNumber Add(BigNumber other)
    {
        int length = Math.Max(blocks.Length, other.blocks.Length);
        var result = new int[length + 1];
        int carry = 0;

        for (int i = 0; i < length; i++)
        {
            int leftBlock = i < blocks.Length ? blocks[i] : 0;
            int rightBlock = i < other.blocks.Length ? other.blocks[i] : 0;
            int sum = leftBlock + rightBlock + carry;

            result[i] = sum % BlockBase;
            carry = sum / BlockBase;
        }

        result[length] = carry;
        return new BigNumber(result);
    }

    private BigNumber Subtract(BigNumber other)
    {
        if (CompareTo(other) < 0)
            throw new InvalidOperationException("BigNumber не поддерживает отрицательные числа.");

        var result = new int[blocks.Length];
        int borrow = 0;

        for (int i = 0; i < blocks.Length; i++)
        {
            int rightBlock = i < other.blocks.Length ? other.blocks[i] : 0;
            int difference = blocks[i] - rightBlock - borrow;

            if (difference < 0)
            {
                difference += BlockBase;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = difference;
        }

        return new BigNumber(result);
    }

    private BigNumber Multiply(BigNumber other)
    {
        var result = new int[blocks.Length + other.blocks.Length];

        // Умножение в столбик: каждый блок первого числа на каждый блок второго.
        for (int i = 0; i < blocks.Length; i++)
        {
            int carry = 0;
            for (int j = 0; j < other.blocks.Length; j++)
            {
                // Промежуточное значение меньше миллиона и помещается в int.
                int product = blocks[i] * other.blocks[j] + result[i + j] + carry;
                result[i + j] = product % BlockBase;
                carry = product / BlockBase;
            }

            result[i + other.blocks.Length] = carry;
        }

        return new BigNumber(result);
    }

    private BigNumber Divide(BigNumber divisor)
    {
        if (divisor.blocks.Length == 1 && divisor.blocks[0] == 0)
            throw new DivideByZeroException("Нельзя делить на ноль.");

        var result = new int[blocks.Length];
        var remainder = new BigNumber(new int[] { 0 });

        // Деление уголком идёт от старшего блока к младшему.
        for (int i = blocks.Length - 1; i >= 0; i--)
        {
            remainder = remainder.AppendBlock(blocks[i]);

            // Следующая цифра частного лежит в диапазоне 0..999.
            int low = 0;
            int high = BlockBase - 1;
            int quotientBlock = 0;

            while (low <= high)
            {
                int middle = (low + high) / 2;
                BigNumber product = divisor.MultiplyBlock(middle);

                if (product.CompareTo(remainder) <= 0)
                {
                    quotientBlock = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            result[i] = quotientBlock;
            remainder = remainder.Subtract(divisor.MultiplyBlock(quotientBlock));
        }

        // Остаток отбрасывается: BigNumber хранит только целые числа.
        return new BigNumber(result);
    }

    private BigNumber MultiplyBlock(int factor)
    {
        var result = new int[blocks.Length + 1];
        int carry = 0;

        for (int i = 0; i < blocks.Length; i++)
        {
            int product = blocks[i] * factor + carry;
            result[i] = product % BlockBase;
            carry = product / BlockBase;
        }

        result[blocks.Length] = carry;
        return new BigNumber(result);
    }

    private BigNumber AppendBlock(int block)
    {
        // remainder * 1000 + block: сдвигаем существующие блоки на одну позицию.
        var result = new int[blocks.Length + 1];
        result[0] = block;
        Array.Copy(blocks, 0, result, 1, blocks.Length);
        return new BigNumber(result);
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(blocks[blocks.Length - 1].ToString(CultureInfo.InvariantCulture));

        // Каждый блок после старшего занимает ровно три десятичные цифры.
        for (int i = blocks.Length - 2; i >= 0; i--)
            result.Append(blocks[i].ToString("D3", CultureInfo.InvariantCulture));

        return result.ToString();
    }
}
