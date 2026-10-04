using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Common;

public static class ZodiacCalculator
{
    /// <summary>
    /// Tính toán Cung Hoàng Đạo dựa vào ngày và tháng sinh.
    /// </summary>
    public static ZodiacSign GetZodiacSign(int day, int month)
    {
        return month switch
        {
            1 => day <= 19 ? ZodiacSign.Capricorn : ZodiacSign.Aquarius,
            2 => day <= 18 ? ZodiacSign.Aquarius : ZodiacSign.Pisces,
            3 => day <= 20 ? ZodiacSign.Pisces : ZodiacSign.Aries,
            4 => day <= 19 ? ZodiacSign.Aries : ZodiacSign.Taurus,
            5 => day <= 20 ? ZodiacSign.Taurus : ZodiacSign.Gemini,
            6 => day <= 21 ? ZodiacSign.Gemini : ZodiacSign.Cancer,
            7 => day <= 22 ? ZodiacSign.Cancer : ZodiacSign.Leo,
            8 => day <= 22 ? ZodiacSign.Leo : ZodiacSign.Virgo,
            9 => day <= 22 ? ZodiacSign.Virgo : ZodiacSign.Libra,
            10 => day <= 23 ? ZodiacSign.Libra : ZodiacSign.Scorpio,
            11 => day <= 21 ? ZodiacSign.Scorpio : ZodiacSign.Sagittarius,
            12 => day <= 21 ? ZodiacSign.Sagittarius : ZodiacSign.Capricorn,
            _ => throw new ArgumentException("Tháng không hợp lệ")
        };
    }
}
