using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

namespace Winotch;

public sealed class AccountPictureService
{
    private const ulong MaxPictureBytes = 1024 * 1024;

    public async Task<byte[]?> ReadAsync()
    {
        try
        {
            var file = UserInformation.GetAccountPicture(AccountPictureKind.SmallImage)
                ?? UserInformation.GetAccountPicture(AccountPictureKind.LargeImage);
            return file is null ? null : await ReadFileAsync(file);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<byte[]?> ReadFileAsync(IStorageFile file)
    {
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        var size = (uint)Math.Min(stream.Size, MaxPictureBytes);
        if (size == 0)
        {
            return null;
        }

        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(size);
        var bytes = new byte[size];
        reader.ReadBytes(bytes);
        return bytes;
    }
}
