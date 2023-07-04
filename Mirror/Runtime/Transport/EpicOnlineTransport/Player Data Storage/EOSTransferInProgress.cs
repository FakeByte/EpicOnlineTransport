using UnityEngine;
using Epic.OnlineServices.PlayerDataStorage;
public class EOSTransferInProgress : MonoBehaviour
{
    public bool Download = true;
    public uint CurrentIndex = 0;
    public byte[] Data;
    private uint transferSize = 0;

    public uint TotalSize
    {
        get
        {
            return transferSize;
        }
        set
        {
            transferSize = value;

            if (transferSize > PlayerDataStorageInterface.FileMaxSizeBytes)
            {
                Debug.LogError("[EOS SDK] Player data storage: data transfer size exceeds max file size.");
                transferSize = PlayerDataStorageInterface.FileMaxSizeBytes;
            }
        }
    }

    public bool Done()
    {
        return TotalSize == CurrentIndex;
    }
}
