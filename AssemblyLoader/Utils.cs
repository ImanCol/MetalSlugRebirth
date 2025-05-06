using System.Text;

namespace AssemblyLoader
{
    public static class Utils
    {
        public static string Md5FromBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;

            // Usar una implementación personalizada de MD5
              byte[]   hash = ComputeMd5Hash(bytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2")); // Formato hexadecimal de 2 dígitos

            return sb.ToString();
        }

        private static byte[] ComputeMd5Hash(byte[] input)
{
    uint a = 0x67452301, b = 0xEFCDAB89, c = 0x98BADCFE, d = 0x10325476;

    int originalLength = input.Length * 8; // Longitud en bits
    int lengthWithPadding = ((input.Length + 8) / 64 + 1) * 64;
    byte[] paddedInput = new byte[lengthWithPadding];

    // Copiar los datos originales al arreglo con relleno
    Array.Copy(input, paddedInput, input.Length);

    // Añadir el bit '1' al final de los datos originales
    paddedInput[input.Length] = 0x80;

    // Asegurar que haya espacio para los últimos 8 bytes
    if (paddedInput.Length < originalLength / 8 + 8)
    {
        throw new ArgumentException("El arreglo de entrada es demasiado pequeño para almacenar la longitud.");
    }

    // Convertir la longitud original a un arreglo de 8 bytes
    byte[] lengthBytes = BitConverter.GetBytes((ulong)originalLength);

    // Copiar los últimos 8 bytes al final del arreglo
    Array.Copy(lengthBytes, 0, paddedInput, paddedInput.Length - 8, 8);

    // Procesar bloques de 64 bytes
    for (int i = 0; i < paddedInput.Length; i += 64)
    {
        uint[] block = new uint[16];
        for (int j = 0; j < 16; j++)
        {
            block[j] = BitConverter.ToUInt32(paddedInput, i + j * 4);
        }

        uint aa = a, bb = b, cc = c, dd = d;

        // Rondas de MD5
        a = FF(a, b, c, d, block[0], 7, 0xD76AA478);
        d = FF(d, a, b, c, block[1], 12, 0xE8C7B756);
        c = FF(c, d, a, b, block[2], 17, 0x242070DB);
        b = FF(b, c, d, a, block[3], 22, 0xC1BDCEEE);
        a = FF(a, b, c, d, block[4], 7, 0xF57C0FAF);
        d = FF(d, a, b, c, block[5], 12, 0x4787C62A);
        c = FF(c, d, a, b, block[6], 17, 0xA8304613);
        b = FF(b, c, d, a, block[7], 22, 0xFD469501);
        a = FF(a, b, c, d, block[8], 7, 0x698098D8);
        d = FF(d, a, b, c, block[9], 12, 0x8B44F7AF);
        c = FF(c, d, a, b, block[10], 17, 0xFFFF5BB1);
        b = FF(b, c, d, a, block[11], 22, 0x895CD7BE);
        a = FF(a, b, c, d, block[12], 7, 0x6B901122);
        d = FF(d, a, b, c, block[13], 12, 0xFD987193);
        c = FF(c, d, a, b, block[14], 17, 0xA679438E);
        b = FF(b, c, d, a, block[15], 22, 0x49B40821);

        // Continuar con las rondas GG, HH, II...

        a += aa;
        b += bb;
        c += cc;
        d += dd;
    }

    byte[] result = new byte[16];
    Array.Copy(BitConverter.GetBytes(a), 0, result, 0, 4);
    Array.Copy(BitConverter.GetBytes(b), 0, result, 4, 4);
    Array.Copy(BitConverter.GetBytes(c), 0, result, 8, 4);
    Array.Copy(BitConverter.GetBytes(d), 0, result, 12, 4);

    return result;
}
        private static uint RotateLeft(uint x, int n)
        {
            return (x << n) | (x >> (32 - n));
        }

        private static uint FF(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            return RotateLeft(a + ((b & c) | (~b & d)) + x + t, s) + b;
        }
    }
}
