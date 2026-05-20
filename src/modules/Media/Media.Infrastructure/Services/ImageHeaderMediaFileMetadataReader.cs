using System.Buffers.Binary;
using Media.Application.Ports.Services.Metadata;
using Media.Domain.Constants;

namespace Media.Infrastructure.Services.Metadata;

public sealed class ImageHeaderMediaFileMetadataReader : IMediaFileMetadataReader
{
    public async Task<MediaFileMetadataResult> ReadAsync(
        Stream content,
        string? contentType,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!ShouldReadImageMetadata(contentType, mediaType))
        {
            return MediaFileMetadataResult.Empty;
        }

        if (!content.CanRead)
        {
            return MediaFileMetadataResult.Empty;
        }

        long? originalPosition = null;

        if (content.CanSeek)
        {
            originalPosition = content.Position;
            content.Position = 0;
        }

        try
        {
            using var memoryStream = new MemoryStream();

            await content.CopyToAsync(memoryStream, cancellationToken);

            byte[] bytes = memoryStream.ToArray();

            if (TryReadPngDimensions(bytes, out int pngWidth, out int pngHeight))
            {
                return new MediaFileMetadataResult
                {
                    Width = pngWidth,
                    Height = pngHeight,
                    DurationSeconds = null
                };
            }

            if (TryReadJpegDimensions(bytes, out int jpegWidth, out int jpegHeight))
            {
                return new MediaFileMetadataResult
                {
                    Width = jpegWidth,
                    Height = jpegHeight,
                    DurationSeconds = null
                };
            }

            if (TryReadGifDimensions(bytes, out int gifWidth, out int gifHeight))
            {
                return new MediaFileMetadataResult
                {
                    Width = gifWidth,
                    Height = gifHeight,
                    DurationSeconds = null
                };
            }

            return MediaFileMetadataResult.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return MediaFileMetadataResult.Empty;
        }
        finally
        {
            if (originalPosition.HasValue && content.CanSeek)
            {
                content.Position = originalPosition.Value;
            }
        }
    }

    private static bool ShouldReadImageMetadata(
        string? contentType,
        string mediaType)
    {
        if (string.Equals(
                mediaType,
                MediaTypes.Image,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadPngDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length < 24)
        {
            return false;
        }

        ReadOnlySpan<byte> signature = stackalloc byte[]
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

        if (!bytes.AsSpan(0, 8).SequenceEqual(signature))
        {
            return false;
        }

        if (bytes[12] != (byte)'I' ||
            bytes[13] != (byte)'H' ||
            bytes[14] != (byte)'D' ||
            bytes[15] != (byte)'R')
        {
            return false;
        }

        width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));

        return width > 0 && height > 0;
    }

    private static bool TryReadGifDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length < 10)
        {
            return false;
        }

        bool isGif =
            bytes[0] == (byte)'G' &&
            bytes[1] == (byte)'I' &&
            bytes[2] == (byte)'F' &&
            bytes[3] == (byte)'8' &&
            (bytes[4] == (byte)'7' || bytes[4] == (byte)'9') &&
            bytes[5] == (byte)'a';

        if (!isGif)
        {
            return false;
        }

        width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
        height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));

        return width > 0 && height > 0;
    }

    private static bool TryReadJpegDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length < 4)
        {
            return false;
        }

        if (bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        int index = 2;

        while (index < bytes.Length)
        {
            while (index < bytes.Length && bytes[index] != 0xFF)
            {
                index++;
            }

            while (index < bytes.Length && bytes[index] == 0xFF)
            {
                index++;
            }

            if (index >= bytes.Length)
            {
                return false;
            }

            byte marker = bytes[index++];

            if (marker == 0xD9 || marker == 0xDA)
            {
                return false;
            }

            if (index + 2 > bytes.Length)
            {
                return false;
            }

            int segmentLength =
                BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(index, 2));

            if (segmentLength < 2)
            {
                return false;
            }

            if (IsStartOfFrameMarker(marker))
            {
                if (index + 7 > bytes.Length)
                {
                    return false;
                }

                height = BinaryPrimitives.ReadUInt16BigEndian(
                    bytes.AsSpan(index + 3, 2));

                width = BinaryPrimitives.ReadUInt16BigEndian(
                    bytes.AsSpan(index + 5, 2));

                return width > 0 && height > 0;
            }

            index += segmentLength;
        }

        return false;
    }

    private static bool IsStartOfFrameMarker(byte marker)
    {
        return marker is
            0xC0 or
            0xC1 or
            0xC2 or
            0xC3 or
            0xC5 or
            0xC6 or
            0xC7 or
            0xC9 or
            0xCA or
            0xCB or
            0xCD or
            0xCE or
            0xCF;
    }
}