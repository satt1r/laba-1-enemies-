using System.Globalization;
using System.Text;

namespace Clicker.Core;

public sealed class BigNumber
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
