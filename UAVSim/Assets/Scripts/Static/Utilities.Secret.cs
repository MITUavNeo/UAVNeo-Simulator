// ───────────────────────────────────────────────────────────────────────────
//  TEMPLATE — copy this file to "Utilities.Secret.cs" (drop the .example),
//  then paste the real autograder AES key in place of the zeros below.
//
//  Utilities.Secret.cs is git-ignored so the key never enters commit history,
//  but it still compiles into local builds — which is required, since the
//  student's build AES-encrypts its score code before submission.
//
//  Ask the course maintainer for the current key. The byte array is the
//  16-byte (128-bit) key, most-significant byte first.
// ───────────────────────────────────────────────────────────────────────────

public static partial class Utilities
{
    private static readonly byte[] key =
    {
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    };
}
