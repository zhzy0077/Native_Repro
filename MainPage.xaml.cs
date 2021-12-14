using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing;
using ZXing.Common;
using ZXing.Common.ReedSolomon;
using ZXing.QrCode.Internal;
using Version = ZXing.QrCode.Internal.Version;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var barcodeWriter = new BarcodeWriter();
            barcodeWriter.Format = BarcodeFormat.QR_CODE;
            barcodeWriter.Options = new EncodingOptions()
            {
                Margin = 1,
                Height = 200,
                Width = 200,
            };

            var img = barcodeWriter.Write("https://www.baidu.com");

            this.img.Source = img;

            var qrcode = Encoder.encode("https://www.baidu.com", ErrorCorrectionLevel.L, new Dictionary<EncodeHintType, object>());
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "12")]
    internal static class Encoder
    {
        private static readonly int[] ALPHANUMERIC_TABLE = new int[96]
        {
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            36,
            -1,
            -1,
            -1,
            37,
            38,
            -1,
            -1,
            -1,
            -1,
            39,
            40,
            -1,
            41,
            42,
            43,
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            44,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            30,
            31,
            32,
            33,
            34,
            35,
            -1,
            -1,
            -1,
            -1,
            -1
        };

        internal static string DEFAULT_BYTE_MODE_ENCODING = "ISO-8859-1";

        private static int calculateMaskPenalty(ByteMatrix matrix)
        {
            return MaskUtil.applyMaskPenaltyRule1(matrix) + MaskUtil.applyMaskPenaltyRule2(matrix) + MaskUtil.applyMaskPenaltyRule3(matrix) + MaskUtil.applyMaskPenaltyRule4(matrix);
        }

        //
        // Summary:
        //     Encode "bytes" with the error correction level "ecLevel". The encoding mode will
        //     be chosen internally by chooseMode(). On success, store the result in "qrCode".
        //     We recommend you to use QRCode.EC_LEVEL_L (the lowest level) for "getECLevel"
        //     since our primary use is to show QR code on desktop screens. We don't need very
        //     strong error correction for this purpose. Note that there is no way to encode
        //     bytes in MODE_KANJI. We might want to add EncodeWithMode() with which clients
        //     can specify the encoding mode. For now, we don't need the functionality.
        //
        // Parameters:
        //   content:
        //     text to encode
        //
        //   ecLevel:
        //     error correction level to use
        //
        // Returns:
        //     ZXing.QrCode.Internal.QRCode representing the encoded QR code
        public static QRCode encode(string content, ErrorCorrectionLevel ecLevel)
        {
            return encode(content, ecLevel, null);
        }

        //
        // Summary:
        //     Encodes the specified content.
        //
        // Parameters:
        //   content:
        //     The content.
        //
        //   ecLevel:
        //     The ec level.
        //
        //   hints:
        //     The hints.
        public static QRCode encode(string content, ErrorCorrectionLevel ecLevel, IDictionary<EncodeHintType, object> hints)
        {
            bool num = hints?.ContainsKey(EncodeHintType.CHARACTER_SET) ?? false;
            string text = (hints == null || !hints.ContainsKey(EncodeHintType.CHARACTER_SET)) ? null : ((string)hints[EncodeHintType.CHARACTER_SET]);
            if (text == null)
            {
                text = DEFAULT_BYTE_MODE_ENCODING;
            }

            bool flag = num || !DEFAULT_BYTE_MODE_ENCODING.Equals(text);
            Mode mode = chooseMode(content, text);
            BitArray bitArray = new BitArray();
            if (mode == Mode.BYTE && flag)
            {
                CharacterSetECI characterSetECIByName = CharacterSetECI.getCharacterSetECIByName(text);
                if (characterSetECIByName != null && (hints == null || !hints.ContainsKey(EncodeHintType.DISABLE_ECI) || hints[EncodeHintType.DISABLE_ECI] == null || !Convert.ToBoolean(hints[EncodeHintType.DISABLE_ECI].ToString())))
                {
                    appendECI(characterSetECIByName, bitArray);
                }
            }

            if ((hints?.ContainsKey(EncodeHintType.GS1_FORMAT) ?? false) && hints[EncodeHintType.GS1_FORMAT] != null && Convert.ToBoolean(hints[EncodeHintType.GS1_FORMAT].ToString()))
            {
                appendModeInfo(Mode.FNC1_FIRST_POSITION, bitArray);
            }

            appendModeInfo(mode, bitArray);
            BitArray bitArray2 = new BitArray();
            appendBytes(content, mode, bitArray2, text);
            Version version;
            if (hints != null && hints.ContainsKey(EncodeHintType.QR_VERSION))
            {
                version = Version.getVersionForNumber(int.Parse(hints[EncodeHintType.QR_VERSION].ToString()));
                if (!willFit(calculateBitsNeeded(mode, bitArray, bitArray2, version), version, ecLevel))
                {
                    throw new WriterException("Data too big for requested version");
                }
            }
            else
            {
                version = recommendVersion(ecLevel, mode, bitArray, bitArray2);
            }

            BitArray bitArray3 = new BitArray();
            bitArray3.appendBitArray(bitArray);
            appendLengthInfo((mode == Mode.BYTE) ? bitArray2.SizeInBytes : content.Length, version, mode, bitArray3);
            bitArray3.appendBitArray(bitArray2);
            Version.ECBlocks eCBlocksForLevel = version.getECBlocksForLevel(ecLevel);
            int numDataBytes = version.TotalCodewords - eCBlocksForLevel.TotalECCodewords;
            terminateBits(numDataBytes, bitArray3);
            BitArray bitArray4 = interleaveWithECBytes(bitArray3, version.TotalCodewords, numDataBytes, eCBlocksForLevel.NumBlocks);
            QRCode obj = new QRCode
            {
                ECLevel = ecLevel,
                Mode = mode,
                Version = version
            };
            int dimensionForVersion = version.DimensionForVersion;
            ByteMatrix matrix = new ByteMatrix(dimensionForVersion, dimensionForVersion);
            int num2 = -1;
            if (hints != null && hints.ContainsKey(EncodeHintType.QR_MASK_PATTERN))
            {
                int num3 = int.Parse(hints[EncodeHintType.QR_MASK_PATTERN].ToString());
                num2 = (QRCode.isValidMaskPattern(num3) ? num3 : (-1));
            }

            if (num2 == -1)
            {
                num2 = chooseMaskPattern(bitArray4, ecLevel, version, matrix);
            }

            obj.MaskPattern = num2;
            MatrixUtil.buildMatrix(bitArray4, ecLevel, version, num2, matrix);
            obj.Matrix = matrix;
            return obj;
        }

        //
        // Summary:
        //     Decides the smallest version of QR code that will contain all of the provided
        //     data.
        //
        // Exceptions:
        //   T:ZXing.WriterException:
        //     if the data cannot fit in any version
        private static Version recommendVersion(ErrorCorrectionLevel ecLevel, Mode mode, BitArray headerBits, BitArray dataBits)
        {
            Version version = chooseVersion(calculateBitsNeeded(mode, headerBits, dataBits, Version.getVersionForNumber(1)), ecLevel);
            return chooseVersion(calculateBitsNeeded(mode, headerBits, dataBits, version), ecLevel);
        }

        private static int calculateBitsNeeded(Mode mode, BitArray headerBits, BitArray dataBits, Version version)
        {
            return headerBits.Size + mode.getCharacterCountBits(version) + dataBits.Size;
        }

        //
        // Summary:
        //     Gets the alphanumeric code.
        //
        // Parameters:
        //   code:
        //     The code.
        //
        // Returns:
        //     the code point of the table used in alphanumeric mode or -1 if there is no corresponding
        //     code in the table.
        internal static int getAlphanumericCode(int code)
        {
            if (code < ALPHANUMERIC_TABLE.Length)
            {
                return ALPHANUMERIC_TABLE[code];
            }

            return -1;
        }

        //
        // Summary:
        //     Chooses the mode.
        //
        // Parameters:
        //   content:
        //     The content.
        public static Mode chooseMode(string content)
        {
            return chooseMode(content, null);
        }

        //
        // Summary:
        //     Choose the best mode by examining the content. Note that 'encoding' is used as
        //     a hint; if it is Shift_JIS, and the input is only double-byte Kanji, then we
        //     return {@link Mode#KANJI}.
        //
        // Parameters:
        //   content:
        //     The content.
        //
        //   encoding:
        //     The encoding.
        private static Mode chooseMode(string content, string encoding)
        {
            if ("Shift_JIS".Equals(encoding) && isOnlyDoubleByteKanji(content))
            {
                return Mode.KANJI;
            }

            bool flag = false;
            bool flag2 = false;
            foreach (char c in content)
            {
                if (c >= '0' && c <= '9')
                {
                    flag = true;
                    continue;
                }

                if (getAlphanumericCode(c) != -1)
                {
                    flag2 = true;
                    continue;
                }

                return Mode.BYTE;
            }

            if (flag2)
            {
                return Mode.ALPHANUMERIC;
            }

            if (flag)
            {
                return Mode.NUMERIC;
            }

            return Mode.BYTE;
        }

        private static bool isOnlyDoubleByteKanji(string content)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(content);
            }
            catch (Exception)
            {
                return false;
            }

            int num = bytes.Length;
            if (num % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < num; i += 2)
            {
                int num2 = bytes[i] & 0xFF;
                if ((num2 < 129 || num2 > 159) && (num2 < 224 || num2 > 235))
                {
                    return false;
                }
            }

            return true;
        }

        private static int chooseMaskPattern(BitArray bits, ErrorCorrectionLevel ecLevel, Version version, ByteMatrix matrix)
        {
            int num = int.MaxValue;
            int result = -1;
            for (int i = 0; i < QRCode.NUM_MASK_PATTERNS; i++)
            {
                MatrixUtil.buildMatrix(bits, ecLevel, version, i, matrix);
                int num2 = calculateMaskPenalty(matrix);
                if (num2 < num)
                {
                    num = num2;
                    result = i;
                }
            }

            return result;
        }

        private static Version chooseVersion(int numInputBits, ErrorCorrectionLevel ecLevel)
        {
            for (int i = 1; i <= 40; i++)
            {
                Version versionForNumber = Version.getVersionForNumber(i);
                if (willFit(numInputBits, versionForNumber, ecLevel))
                {
                    return versionForNumber;
                }
            }

            throw new WriterException("Data too big");
        }

        //
        // Returns:
        //     true if the number of input bits will fit in a code with the specified version
        //     and error correction level.
        private static bool willFit(int numInputBits, Version version, ErrorCorrectionLevel ecLevel)
        {
            int totalCodewords = version.TotalCodewords;
            int totalECCodewords = version.getECBlocksForLevel(ecLevel).TotalECCodewords;
            int num = totalCodewords - totalECCodewords;
            int num2 = (numInputBits + 7) / 8;
            return num >= num2;
        }

        //
        // Summary:
        //     Terminate bits as described in 8.4.8 and 8.4.9 of JISX0510:2004 (p.24).
        //
        // Parameters:
        //   numDataBytes:
        //     The num data bytes.
        //
        //   bits:
        //     The bits.
        internal static void terminateBits(int numDataBytes, BitArray bits)
        {
            int num = numDataBytes << 3;
            if (bits.Size > num)
            {
                throw new WriterException("data bits cannot fit in the QR Code" + bits.Size + " > " + num);
            }

            for (int i = 0; i < 4; i++)
            {
                if (bits.Size >= num)
                {
                    break;
                }

                bits.appendBit(bit: false);
            }

            int num2 = bits.Size & 7;
            if (num2 > 0)
            {
                for (int j = num2; j < 8; j++)
                {
                    bits.appendBit(bit: false);
                }
            }

            int num3 = numDataBytes - bits.SizeInBytes;
            for (int k = 0; k < num3; k++)
            {
                bits.appendBits(((k & 1) == 0) ? 236 : 17, 8);
            }

            if (bits.Size != num)
            {
                throw new WriterException("Bits size does not equal capacity");
            }
        }

        //
        // Summary:
        //     Get number of data bytes and number of error correction bytes for block id "blockID".
        //     Store the result in "numDataBytesInBlock", and "numECBytesInBlock". See table
        //     12 in 8.5.1 of JISX0510:2004 (p.30)
        //
        // Parameters:
        //   numTotalBytes:
        //     The num total bytes.
        //
        //   numDataBytes:
        //     The num data bytes.
        //
        //   numRSBlocks:
        //     The num RS blocks.
        //
        //   blockID:
        //     The block ID.
        //
        //   numDataBytesInBlock:
        //     The num data bytes in block.
        //
        //   numECBytesInBlock:
        //     The num EC bytes in block.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "<>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0048:Add parentheses for clarity", Justification = "<>")]
        internal static void getNumDataBytesAndNumECBytesForBlockID(int numTotalBytes, int numDataBytes, int numRSBlocks, int blockID, int[] numDataBytesInBlock, int[] numECBytesInBlock)
        {
            if (blockID >= numRSBlocks)
            {
                throw new WriterException("Block ID too large");
            }

            int num = numTotalBytes % numRSBlocks;
            int num2 = numRSBlocks - num;
            int num3 = numTotalBytes / numRSBlocks;
            int num4 = num3 + 1;
            int num5 = numDataBytes / numRSBlocks;
            int num6 = num5 + 1;
            int num7 = num3 - num5;
            int num8 = num4 - num6;
            if (num7 != num8)
            {
                throw new WriterException("EC bytes mismatch");
            }

            if (numRSBlocks != num2 + num)
            {
                throw new WriterException("RS blocks mismatch");
            }

            if (numTotalBytes != (num5 + num7) * num2 + (num6 + num8) * num)
            {
                throw new WriterException("Total bytes mismatch");
            }

            if (blockID < num2)
            {
                numDataBytesInBlock[0] = num5;
                numECBytesInBlock[0] = num7;
            }
            else
            {
                numDataBytesInBlock[0] = num6;
                numECBytesInBlock[0] = num8;
            }
        }

        //
        // Summary:
        //     Interleave "bits" with corresponding error correction bytes. On success, store
        //     the result in "result". The interleave rule is complicated. See 8.6 of JISX0510:2004
        //     (p.37) for details.
        //
        // Parameters:
        //   bits:
        //     The bits.
        //
        //   numTotalBytes:
        //     The num total bytes.
        //
        //   numDataBytes:
        //     The num data bytes.
        //
        //   numRSBlocks:
        //     The num RS blocks.
        internal static BitArray interleaveWithECBytes(BitArray bits, int numTotalBytes, int numDataBytes, int numRSBlocks)
        {
            if (bits.SizeInBytes != numDataBytes)
            {
                throw new WriterException("Number of bits and data bytes does not match");
            }

            int num = 0;
            int num2 = 0;
            int num3 = 0;
            List<BlockPair> list = new List<BlockPair>(numRSBlocks);
            for (int i = 0; i < numRSBlocks; i++)
            {
                int[] array = new int[1];
                int[] array2 = new int[1];
                getNumDataBytesAndNumECBytesForBlockID(numTotalBytes, numDataBytes, numRSBlocks, i, array, array2);
                int num4 = array[0];
                byte[] array3 = new byte[num4];
                bits.toBytes(8 * num, array3, 0, num4);
                byte[] array4 = generateECBytes(array3, array2[0]);
                list.Add(new BlockPair(array3, array4));
                num2 = Math.Max(num2, num4);
                num3 = Math.Max(num3, array4.Length);
                num += array[0];
            }

            if (numDataBytes != num)
            {
                throw new WriterException("Data bytes does not match offset");
            }

            BitArray bitArray = new BitArray();
            for (int j = 0; j < num2; j++)
            {
                foreach (BlockPair item in list)
                {
                    byte[] dataBytes = item.DataBytes;
                    if (j < dataBytes.Length)
                    {
                        bitArray.appendBits(dataBytes[j], 8);
                    }
                }
            }

            for (int k = 0; k < num3; k++)
            {
                foreach (BlockPair item2 in list)
                {
                    byte[] errorCorrectionBytes = item2.ErrorCorrectionBytes;
                    if (k < errorCorrectionBytes.Length)
                    {
                        bitArray.appendBits(errorCorrectionBytes[k], 8);
                    }
                }
            }

            if (numTotalBytes != bitArray.SizeInBytes)
            {
                throw new WriterException("Interleaving error: " + numTotalBytes + " and " + bitArray.SizeInBytes + " differ.");
            }

            return bitArray;
        }

        internal static byte[] generateECBytes(byte[] dataBytes, int numEcBytesInBlock)
        {
            int num = dataBytes.Length;
            int[] array = new int[num + numEcBytesInBlock];
            for (int i = 0; i < num; i++)
            {
                array[i] = (dataBytes[i] & 0xFF);
            }

            new ReedSolomonEncoder(GenericGF.QR_CODE_FIELD_256).encode(array, numEcBytesInBlock);
            byte[] array2 = new byte[numEcBytesInBlock];
            for (int j = 0; j < numEcBytesInBlock; j++)
            {
                array2[j] = (byte)array[num + j];
            }

            return array2;
        }

        //
        // Summary:
        //     Append mode info. On success, store the result in "bits".
        //
        // Parameters:
        //   mode:
        //     The mode.
        //
        //   bits:
        //     The bits.
        internal static void appendModeInfo(Mode mode, BitArray bits)
        {
            bits.appendBits(mode.Bits, 4);
        }

        //
        // Summary:
        //     Append length info. On success, store the result in "bits".
        //
        // Parameters:
        //   numLetters:
        //     The num letters.
        //
        //   version:
        //     The version.
        //
        //   mode:
        //     The mode.
        //
        //   bits:
        //     The bits.
        internal static void appendLengthInfo(int numLetters, Version version, Mode mode, BitArray bits)
        {
            int characterCountBits = mode.getCharacterCountBits(version);
            if (numLetters >= 1 << characterCountBits)
            {
                throw new WriterException(numLetters + " is bigger than " + ((1 << characterCountBits) - 1));
            }

            bits.appendBits(numLetters, characterCountBits);
        }

        //
        // Summary:
        //     Append "bytes" in "mode" mode (encoding) into "bits". On success, store the result
        //     in "bits".
        //
        // Parameters:
        //   content:
        //     The content.
        //
        //   mode:
        //     The mode.
        //
        //   bits:
        //     The bits.
        //
        //   encoding:
        //     The encoding.
        internal static void appendBytes(string content, Mode mode, BitArray bits, string encoding)
        {
            if (mode.Equals(Mode.NUMERIC))
            {
                appendNumericBytes(content, bits);
                return;
            }

            if (mode.Equals(Mode.ALPHANUMERIC))
            {
                appendAlphanumericBytes(content, bits);
                return;
            }

            if (mode.Equals(Mode.BYTE))
            {
                append8BitBytes(content, bits, encoding);
                return;
            }

            if (mode.Equals(Mode.KANJI))
            {
                appendKanjiBytes(content, bits);
                return;
            }

            throw new WriterException("Invalid mode: " + mode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "<>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0048:Add parentheses for clarity", Justification = "<>")]
        internal static void appendNumericBytes(string content, BitArray bits)
        {
            int length = content.Length;
            int num = 0;
            while (num < length)
            {
                int num2 = content[num] - 48;
                if (num + 2 < length)
                {
                    int num3 = content[num + 1] - 48;
                    int num4 = content[num + 2] - 48;
                    bits.appendBits(num2 * 100 + num3 * 10 + num4, 10);
                    num += 3;
                }
                else if (num + 1 < length)
                {
                    int num5 = content[num + 1] - 48;
                    bits.appendBits(num2 * 10 + num5, 7);
                    num += 2;
                }
                else
                {
                    bits.appendBits(num2, 4);
                    num++;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "<>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0048:Add parentheses for clarity", Justification = "<>")]
        internal static void appendAlphanumericBytes(string content, BitArray bits)
        {
            int length = content.Length;
            int num = 0;
            while (num < length)
            {
                int alphanumericCode = getAlphanumericCode(content[num]);
                if (alphanumericCode == -1)
                {
                    throw new WriterException();
                }

                if (num + 1 < length)
                {
                    int alphanumericCode2 = getAlphanumericCode(content[num + 1]);
                    if (alphanumericCode2 == -1)
                    {
                        throw new WriterException();
                    }

                    bits.appendBits(alphanumericCode * 45 + alphanumericCode2, 11);
                    num += 2;
                }
                else
                {
                    bits.appendBits(alphanumericCode, 6);
                    num++;
                }
            }
        }

        internal static void append8BitBytes(string content, BitArray bits, string encoding)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding(encoding).GetBytes(content);
            }
            catch (Exception ex)
            {
                throw new WriterException(ex.Message, ex);
            }

            byte[] array = bytes;
            foreach (byte value in array)
            {
                bits.appendBits(value, 8);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "<12>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0048:Add parentheses for clarity", Justification = "<>")]
        internal static void appendKanjiBytes(string content, BitArray bits)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(content);
            }
            catch (Exception ex)
            {
                throw new WriterException(ex.Message, ex);
            }

            if (bytes.Length % 2 != 0)
            {
                throw new WriterException("Kanji byte size not even");
            }

            int num = bytes.Length - 1;
            for (int i = 0; i < num; i += 2)
            {
                int num2 = bytes[i] & 0xFF;
                int num3 = bytes[i + 1] & 0xFF;
                int num4 = (num2 << 8) | num3;
                int num5 = -1;
                if (num4 >= 33088 && num4 <= 40956)
                {
                    num5 = num4 - 33088;
                }
                else if (num4 >= 57408 && num4 <= 60351)
                {
                    num5 = num4 - 49472;
                }

                if (num5 == -1)
                {
                    throw new WriterException("Invalid byte sequence");
                }

                int value = (num5 >> 8) * 192 + (num5 & 0xFF);
                bits.appendBits(value, 13);
            }
        }

        private static void appendECI(CharacterSetECI eci, BitArray bits)
        {
            bits.appendBits(Mode.ECI.Bits, 4);
            bits.appendBits(eci.Value, 8);
        }
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<>")]
    internal sealed class BlockPair
    {
        private readonly byte[] dataBytes;

        private readonly byte[] errorCorrectionBytes;

        public byte[] DataBytes => dataBytes;

        public byte[] ErrorCorrectionBytes => errorCorrectionBytes;

        public BlockPair(byte[] data, byte[] errorCorrection)
        {
            dataBytes = data;
            errorCorrectionBytes = errorCorrection;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "12")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0048:Add parentheses for clarity", Justification = "<>")]
    internal class MatrixUtil
    {
        private static readonly int[][] POSITION_DETECTION_PATTERN = new int[7][]
        {
            new int[7]
            {
                1,
                1,
                1,
                1,
                1,
                1,
                1
            },
            new int[7]
            {
                1,
                0,
                0,
                0,
                0,
                0,
                1
            },
            new int[7]
            {
                1,
                0,
                1,
                1,
                1,
                0,
                1
            },
            new int[7]
            {
                1,
                0,
                1,
                1,
                1,
                0,
                1
            },
            new int[7]
            {
                1,
                0,
                1,
                1,
                1,
                0,
                1
            },
            new int[7]
            {
                1,
                0,
                0,
                0,
                0,
                0,
                1
            },
            new int[7]
            {
                1,
                1,
                1,
                1,
                1,
                1,
                1
            }
        };

        private static readonly int[][] POSITION_ADJUSTMENT_PATTERN = new int[5][]
        {
            new int[5]
            {
                1,
                1,
                1,
                1,
                1
            },
            new int[5]
            {
                1,
                0,
                0,
                0,
                1
            },
            new int[5]
            {
                1,
                0,
                1,
                0,
                1
            },
            new int[5]
            {
                1,
                0,
                0,
                0,
                1
            },
            new int[5]
            {
                1,
                1,
                1,
                1,
                1
            }
        };

        private static readonly int[][] POSITION_ADJUSTMENT_PATTERN_COORDINATE_TABLE = new int[40][]
        {
            new int[7]
            {
                -1,
                -1,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                18,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                22,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                34,
                -1,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                22,
                38,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                24,
                42,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                46,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                28,
                50,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                54,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                32,
                58,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                34,
                62,
                -1,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                46,
                66,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                48,
                70,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                50,
                74,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                54,
                78,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                56,
                82,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                58,
                86,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                34,
                62,
                90,
                -1,
                -1,
                -1
            },
            new int[7]
            {
                6,
                28,
                50,
                72,
                94,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                50,
                74,
                98,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                54,
                78,
                102,
                -1,
                -1
            },
            new int[7]
            {
                6,
                28,
                54,
                80,
                106,
                -1,
                -1
            },
            new int[7]
            {
                6,
                32,
                58,
                84,
                110,
                -1,
                -1
            },
            new int[7]
            {
                6,
                30,
                58,
                86,
                114,
                -1,
                -1
            },
            new int[7]
            {
                6,
                34,
                62,
                90,
                118,
                -1,
                -1
            },
            new int[7]
            {
                6,
                26,
                50,
                74,
                98,
                122,
                -1
            },
            new int[7]
            {
                6,
                30,
                54,
                78,
                102,
                126,
                -1
            },
            new int[7]
            {
                6,
                26,
                52,
                78,
                104,
                130,
                -1
            },
            new int[7]
            {
                6,
                30,
                56,
                82,
                108,
                134,
                -1
            },
            new int[7]
            {
                6,
                34,
                60,
                86,
                112,
                138,
                -1
            },
            new int[7]
            {
                6,
                30,
                58,
                86,
                114,
                142,
                -1
            },
            new int[7]
            {
                6,
                34,
                62,
                90,
                118,
                146,
                -1
            },
            new int[7]
            {
                6,
                30,
                54,
                78,
                102,
                126,
                150
            },
            new int[7]
            {
                6,
                24,
                50,
                76,
                102,
                128,
                154
            },
            new int[7]
            {
                6,
                28,
                54,
                80,
                106,
                132,
                158
            },
            new int[7]
            {
                6,
                32,
                58,
                84,
                110,
                136,
                162
            },
            new int[7]
            {
                6,
                26,
                54,
                82,
                110,
                138,
                166
            },
            new int[7]
            {
                6,
                30,
                58,
                86,
                114,
                142,
                170
            }
        };

        private static readonly int[][] TYPE_INFO_COORDINATES = new int[15][]
        {
            new int[2]
            {
                8,
                0
            },
            new int[2]
            {
                8,
                1
            },
            new int[2]
            {
                8,
                2
            },
            new int[2]
            {
                8,
                3
            },
            new int[2]
            {
                8,
                4
            },
            new int[2]
            {
                8,
                5
            },
            new int[2]
            {
                8,
                7
            },
            new int[2]
            {
                8,
                8
            },
            new int[2]
            {
                7,
                8
            },
            new int[2]
            {
                5,
                8
            },
            new int[2]
            {
                4,
                8
            },
            new int[2]
            {
                3,
                8
            },
            new int[2]
            {
                2,
                8
            },
            new int[2]
            {
                1,
                8
            },
            new int[2]
            {
                0,
                8
            }
        };

        private const int VERSION_INFO_POLY = 7973;

        private const int TYPE_INFO_POLY = 1335;

        private const int TYPE_INFO_MASK_PATTERN = 21522;

        //
        // Summary:
        //     Set all cells to 2. 2 means that the cell is empty (not set yet). JAVAPORT: We
        //     shouldn't need to do this at all. The code should be rewritten to begin encoding
        //     with the ByteMatrix initialized all to zero.
        //
        // Parameters:
        //   matrix:
        //     The matrix.
        public static void clearMatrix(ByteMatrix matrix)
        {
            matrix.clear(2);
        }

        //
        // Summary:
        //     Build 2D matrix of QR Code from "dataBits" with "ecLevel", "version" and "getMaskPattern".
        //     On success, store the result in "matrix" and return true.
        //
        // Parameters:
        //   dataBits:
        //     The data bits.
        //
        //   ecLevel:
        //     The ec level.
        //
        //   version:
        //     The version.
        //
        //   maskPattern:
        //     The mask pattern.
        //
        //   matrix:
        //     The matrix.
        public static void buildMatrix(BitArray dataBits, ErrorCorrectionLevel ecLevel, Version version, int maskPattern, ByteMatrix matrix)
        {
            clearMatrix(matrix);
            embedBasicPatterns(version, matrix);
            embedTypeInfo(ecLevel, maskPattern, matrix);
            maybeEmbedVersionInfo(version, matrix);
            embedDataBits(dataBits, maskPattern, matrix);
        }

        //
        // Summary:
        //     Embed basic patterns. On success, modify the matrix and return true. The basic
        //     patterns are: - Position detection patterns - Timing patterns - Dark dot at the
        //     left bottom corner - Position adjustment patterns, if need be
        //
        // Parameters:
        //   version:
        //     The version.
        //
        //   matrix:
        //     The matrix.
        public static void embedBasicPatterns(Version version, ByteMatrix matrix)
        {
            embedPositionDetectionPatternsAndSeparators(matrix);
            embedDarkDotAtLeftBottomCorner(matrix);
            maybeEmbedPositionAdjustmentPatterns(version, matrix);
            embedTimingPatterns(matrix);
        }

        //
        // Summary:
        //     Embed type information. On success, modify the matrix.
        //
        // Parameters:
        //   ecLevel:
        //     The ec level.
        //
        //   maskPattern:
        //     The mask pattern.
        //
        //   matrix:
        //     The matrix.
        public static void embedTypeInfo(ErrorCorrectionLevel ecLevel, int maskPattern, ByteMatrix matrix)
        {
            BitArray bitArray = new BitArray();
            makeTypeInfoBits(ecLevel, maskPattern, bitArray);
            for (int i = 0; i < bitArray.Size; i++)
            {
                int value = bitArray[bitArray.Size - 1 - i] ? 1 : 0;
                int[] obj = TYPE_INFO_COORDINATES[i];
                int x = obj[0];
                int y = obj[1];
                matrix[x, y] = value;
                int x2;
                int y2;
                if (i < 8)
                {
                    x2 = matrix.Width - i - 1;
                    y2 = 8;
                }
                else
                {
                    x2 = 8;
                    y2 = matrix.Height - 7 + (i - 8);
                }

                matrix[x2, y2] = value;
            }
        }

        //
        // Summary:
        //     Embed version information if need be. On success, modify the matrix and return
        //     true. See 8.10 of JISX0510:2004 (p.47) for how to embed version information.
        //
        // Parameters:
        //   version:
        //     The version.
        //
        //   matrix:
        //     The matrix.
        public static void maybeEmbedVersionInfo(Version version, ByteMatrix matrix)
        {
            if (version.VersionNumber < 7)
            {
                return;
            }

            BitArray bitArray = new BitArray();
            makeVersionInfoBits(version, bitArray);
            int num = 17;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int value = bitArray[num] ? 1 : 0;
                    num--;
                    matrix[i, matrix.Height - 11 + j] = value;
                    matrix[matrix.Height - 11 + j, i] = value;
                }
            }
        }

        //
        // Summary:
        //     Embed "dataBits" using "getMaskPattern". On success, modify the matrix and return
        //     true. For debugging purposes, it skips masking process if "getMaskPattern" is
        //     -1. See 8.7 of JISX0510:2004 (p.38) for how to embed data bits.
        //
        // Parameters:
        //   dataBits:
        //     The data bits.
        //
        //   maskPattern:
        //     The mask pattern.
        //
        //   matrix:
        //     The matrix.
        public static void embedDataBits(BitArray dataBits, int maskPattern, ByteMatrix matrix)
        {
            var rootFolder = ApplicationData.Current.LocalFolder;
            var logFolderName = "Logger";
            var fullPath = $"{rootFolder.Path}\\embedDataBits.log";
            int num = 0;
            int num2 = -1;
            int num3 = matrix.Width - 1;
            int i = matrix.Height - 1;
            while (num3 > 0)
            {
                if (num3 == 6)
                {
                    num3--;
                }

                for (; i >= 0 && i < matrix.Height; i += num2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = num3 - j;
                        if (isEmpty(matrix[x, i]))
                        {
                            int num4;
                            if (num < dataBits.Size)
                            {
                                File.AppendAllText(fullPath, $"{x} - {i} - {dataBits[num]}\n");
                            }

                            if (num < dataBits.Size)
                            {
                                num4 = (dataBits[num] ? 1 : 0);
                                num++;
                            }
                            else
                            {
                                num4 = 0;
                            }

                            File.AppendAllText(fullPath, $"{x} - {i} - {num4}\n");
                            if (maskPattern != -1 && MaskUtil.getDataMaskBit(maskPattern, x, i))
                            {
                                num4 ^= 1;
                            }

                            matrix[x, i] = num4;
                        }
                    }
                }

                num2 = -num2;
                i += num2;
                num3 -= 2;
            }

            if (num != dataBits.Size)
            {
                throw new WriterException("Not all bits consumed: " + num + "/" + dataBits.Size);
            }
        }

        //
        // Summary:
        //     Return the position of the most significant bit set (to one) in the "value".
        //     The most significant bit is position 32. If there is no bit set, return 0. Examples:
        //     - findMSBSet(0) => 0 - findMSBSet(1) => 1 - findMSBSet(255) => 8
        //
        // Parameters:
        //   value_Renamed:
        //     The value_ renamed.
        public static int findMSBSet(int value_Renamed)
        {
            int num = 0;
            while (value_Renamed != 0)
            {
                value_Renamed = (int)((uint)value_Renamed >> 1);
                num++;
            }

            return num;
        }

        //
        // Summary:
        //     Calculate BCH (Bose-Chaudhuri-Hocquenghem) code for "value" using polynomial
        //     "poly". The BCH code is used for encoding type information and version information.
        //     Example: Calculation of version information of 7. f(x) is created from 7. - 7
        //     = 000111 in 6 bits - f(x) = x^2 + x^2 + x^1 g(x) is given by the standard (p.
        //     67) - g(x) = x^12 + x^11 + x^10 + x^9 + x^8 + x^5 + x^2 + 1 Multiply f(x) by
        //     x^(18 - 6) - f'(x) = f(x) * x^(18 - 6) - f'(x) = x^14 + x^13 + x^12 Calculate
        //     the remainder of f'(x) / g(x) x^2 __________________________________________________
        //     g(x) )x^14 + x^13 + x^12 x^14 + x^13 + x^12 + x^11 + x^10 + x^7 + x^4 + x^2 --------------------------------------------------
        //     x^11 + x^10 + x^7 + x^4 + x^2 The remainder is x^11 + x^10 + x^7 + x^4 + x^2
        //     Encode it in binary: 110010010100 The return value is 0xc94 (1100 1001 0100)
        //     Since all coefficients in the polynomials are 1 or 0, we can do the calculation
        //     by bit operations. We don't care if coefficients are positive or negative.
        //
        // Parameters:
        //   value:
        //     The value.
        //
        //   poly:
        //     The poly.
        public static int calculateBCHCode(int value, int poly)
        {
            if (poly == 0)
            {
                throw new ArgumentException("0 polynominal", "poly");
            }

            int num = findMSBSet(poly);
            value <<= num - 1;
            while (findMSBSet(value) >= num)
            {
                value ^= poly << findMSBSet(value) - num;
            }

            return value;
        }

        //
        // Summary:
        //     Make bit vector of type information. On success, store the result in "bits" and
        //     return true. Encode error correction level and mask pattern. See 8.9 of JISX0510:2004
        //     (p.45) for details.
        //
        // Parameters:
        //   ecLevel:
        //     The ec level.
        //
        //   maskPattern:
        //     The mask pattern.
        //
        //   bits:
        //     The bits.
        public static void makeTypeInfoBits(ErrorCorrectionLevel ecLevel, int maskPattern, BitArray bits)
        {
            if (!QRCode.isValidMaskPattern(maskPattern))
            {
                throw new WriterException("Invalid mask pattern");
            }

            int value = (ecLevel.Bits << 3) | maskPattern;
            bits.appendBits(value, 5);
            int value2 = calculateBCHCode(value, 1335);
            bits.appendBits(value2, 10);
            BitArray bitArray = new BitArray();
            bitArray.appendBits(21522, 15);
            bits.xor(bitArray);
            if (bits.Size != 15)
            {
                throw new WriterException("should not happen but we got: " + bits.Size);
            }
        }

        //
        // Summary:
        //     Make bit vector of version information. On success, store the result in "bits"
        //     and return true. See 8.10 of JISX0510:2004 (p.45) for details.
        //
        // Parameters:
        //   version:
        //     The version.
        //
        //   bits:
        //     The bits.
        public static void makeVersionInfoBits(Version version, BitArray bits)
        {
            bits.appendBits(version.VersionNumber, 6);
            int value = calculateBCHCode(version.VersionNumber, 7973);
            bits.appendBits(value, 12);
            if (bits.Size != 18)
            {
                throw new WriterException("should not happen but we got: " + bits.Size);
            }
        }

        //
        // Summary:
        //     Check if "value" is empty.
        //
        // Parameters:
        //   value:
        //     The value.
        //
        // Returns:
        //     true if the specified value is empty; otherwise, false.
        private static bool isEmpty(int value)
        {
            return value == 2;
        }

        private static void embedTimingPatterns(ByteMatrix matrix)
        {
            for (int i = 8; i < matrix.Width - 8; i++)
            {
                int value = (i + 1) % 2;
                if (isEmpty(matrix[i, 6]))
                {
                    matrix[i, 6] = value;
                }

                if (isEmpty(matrix[6, i]))
                {
                    matrix[6, i] = value;
                }
            }
        }

        //
        // Summary:
        //     Embed the lonely dark dot at left bottom corner. JISX0510:2004 (p.46)
        //
        // Parameters:
        //   matrix:
        //     The matrix.
        private static void embedDarkDotAtLeftBottomCorner(ByteMatrix matrix)
        {
            if (matrix[8, matrix.Height - 8] == 0)
            {
                throw new WriterException();
            }

            matrix[8, matrix.Height - 8] = 1;
        }

        private static void embedHorizontalSeparationPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int i = 0; i < 8; i++)
            {
                if (!isEmpty(matrix[xStart + i, yStart]))
                {
                    throw new WriterException();
                }

                matrix[xStart + i, yStart] = 0;
            }
        }

        private static void embedVerticalSeparationPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int i = 0; i < 7; i++)
            {
                if (!isEmpty(matrix[xStart, yStart + i]))
                {
                    throw new WriterException();
                }

                matrix[xStart, yStart + i] = 0;
            }
        }

        //
        // Parameters:
        //   xStart:
        //     The x start.
        //
        //   yStart:
        //     The y start.
        //
        //   matrix:
        //     The matrix.
        private static void embedPositionAdjustmentPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int i = 0; i < 5; i++)
            {
                int[] array = POSITION_ADJUSTMENT_PATTERN[i];
                for (int j = 0; j < 5; j++)
                {
                    matrix[xStart + j, yStart + i] = array[j];
                }
            }
        }

        private static void embedPositionDetectionPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int i = 0; i < 7; i++)
            {
                int[] array = POSITION_DETECTION_PATTERN[i];
                for (int j = 0; j < 7; j++)
                {
                    matrix[xStart + j, yStart + i] = array[j];
                }
            }
        }

        //
        // Summary:
        //     Embed position detection patterns and surrounding vertical/horizontal separators.
        //
        // Parameters:
        //   matrix:
        //     The matrix.
        private static void embedPositionDetectionPatternsAndSeparators(ByteMatrix matrix)
        {
            int num = POSITION_DETECTION_PATTERN[0].Length;
            embedPositionDetectionPattern(0, 0, matrix);
            embedPositionDetectionPattern(matrix.Width - num, 0, matrix);
            embedPositionDetectionPattern(0, matrix.Width - num, matrix);
            embedHorizontalSeparationPattern(0, 7, matrix);
            embedHorizontalSeparationPattern(matrix.Width - 8, 7, matrix);
            embedHorizontalSeparationPattern(0, matrix.Width - 8, matrix);
            embedVerticalSeparationPattern(7, 0, matrix);
            embedVerticalSeparationPattern(matrix.Height - 7 - 1, 0, matrix);
            embedVerticalSeparationPattern(7, matrix.Height - 7, matrix);
        }

        //
        // Summary:
        //     Embed position adjustment patterns if need be.
        //
        // Parameters:
        //   version:
        //     The version.
        //
        //   matrix:
        //     The matrix.
        private static void maybeEmbedPositionAdjustmentPatterns(Version version, ByteMatrix matrix)
        {
            if (version.VersionNumber < 2)
            {
                return;
            }

            int num = version.VersionNumber - 1;
            int[] array = POSITION_ADJUSTMENT_PATTERN_COORDINATE_TABLE[num];
            int[] array2 = array;
            foreach (int num2 in array2)
            {
                if (num2 < 0)
                {
                    continue;
                }

                int[] array3 = array;
                foreach (int num3 in array3)
                {
                    if (num3 >= 0 && isEmpty(matrix[num3, num2]))
                    {
                        embedPositionAdjustmentPattern(num3 - 2, num2 - 2, matrix);
                    }
                }
            }
        }
    }
}
