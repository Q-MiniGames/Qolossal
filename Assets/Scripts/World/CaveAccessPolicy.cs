// User-approved 2026-10-30: MR01-MR03 each use C06 as their sole surface entrance/exit.
// C01-C05 remain playable rooms; their rewards, discoveries and save IDs are retained.
public static class CaveAccessPolicy
{
    public static bool IsRetiredEntrance(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return false;
        roomId = roomId.Replace('-', '_');
        return roomId.Length == 8 && (roomId.StartsWith("MR01_") || roomId.StartsWith("MR02_") || roomId.StartsWith("MR03_"))
            && roomId[5] == 'C' && roomId[6] == '0' && roomId[7] >= '1' && roomId[7] <= '5';
    }
}
