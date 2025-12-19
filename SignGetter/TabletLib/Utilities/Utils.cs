namespace TabletLib.Utilities;

public static class Utils
{
    public static object ReadCustomSize(byte[] data, int byteOffset, int sizeInBytes, bool isSigned)
    {
        if (isSigned)
        {
            long value = 0;
            for (int i = 0; i < sizeInBytes; i++)
            {
                value |= (long)data[byteOffset + i] << (i * 8);
            }
        
            int signBitPos = sizeInBytes * 8 - 1;
            if ((value & (1L << signBitPos)) != 0)
            {
                value |= ~((1L << (sizeInBytes * 8)) - 1);
            }
        
            return value;
        }
        else
        {
            ulong value = 0;
            for (int i = 0; i < sizeInBytes; i++)
            {
                value |= (ulong)data[byteOffset + i] << (i * 8);
            }
            return value;
        }
    }
    
    
    public static object ReadBytes(byte[] data, int byteOffset, int sizeInBits, bool isSigned)
    {
        if (byteOffset + sizeInBits > data.Length)
            throw new ArgumentOutOfRangeException(nameof(data), "Byte offset is out of bounds");

        switch (sizeInBits)
        {
            case 8:
                if (isSigned)
                    return (sbyte)data[byteOffset];
                else
                    return data[byteOffset];

            case 16:
                if (isSigned)
                    return BitConverter.ToInt16(data, byteOffset);
                else
                    return BitConverter.ToUInt16(data, byteOffset);

            case 32:
                if (isSigned)
                    return BitConverter.ToInt32(data, byteOffset);
                else
                    return BitConverter.ToUInt32(data, byteOffset);

            case 64:
                if (isSigned)
                    return BitConverter.ToInt64(data, byteOffset);
                else
                    return BitConverter.ToUInt64(data, byteOffset);

            default:
                return ReadCustomSize(data, byteOffset, sizeInBits, isSigned);
        }
    }
}