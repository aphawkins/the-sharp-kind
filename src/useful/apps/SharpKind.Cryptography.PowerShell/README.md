# SharpKind.Cryptography

A PowerShell module that encrypts, decrypts and cracks classical ciphers. It wraps the
`SharpKind.Security.Cryptography` and `SharpKind.Security.Cryptography.Cryptanalysis` libraries.

Requires PowerShell 7.6 or later: the libraries target .NET 10.

## Install

From the PowerShell Gallery:

```powershell
Install-PSResource SharpKind.Cryptography
```

Or download `SharpKind.Cryptography-<version>.zip` from a
[GitHub release](https://github.com/aphawkins/the-sharp-kind/releases) and extract it into a folder on
`$env:PSModulePath`.

## Commands

| Command                | Does                                                        |
| ---------------------- | ----------------------------------------------------------- |
| `New-CipherSettings`   | Gets a cipher's default settings, or random ones with `-Random`. |
| `Protect-CipherText`   | Encrypts text.                                              |
| `Unprotect-CipherText` | Decrypts text.                                              |
| `Find-CipherKey`       | Cracks Caesar, MonoAlphabetic or Homophonic ciphertext.     |

`-Settings` takes a settings object from `New-CipherSettings` or `Find-CipherKey`, or a hashtable:

| Cipher                         | Hashtable keys                                              |
| ------------------------------ | ----------------------------------------------------------- |
| Adfgvx                         | `Labels`, `Square`, `Keyword`                               |
| Affine                         | `Multiplier`, `Shift`                                       |
| Atbash, Rot13                  | none                                                        |
| Autokey, Beaufort, Vigenere    | `Keyword`                                                   |
| Book                           | `KeyText`                                                   |
| Caesar                         | `RightShift`                                                |
| Columnar, Playfair             | `Keyword`                                                   |
| Enigma                         | `Reflector`, `Rotors`, `Rings`, `Positions`, `Plugboard`    |
| Homophonic                     | one key per plaintext letter, each with its homophones      |
| MonoAlphabetic, Reflector      | `CharacterSet`, `Substitutions`                             |
| RailFence                      | `Rails`                                                     |

Enigma's rotors, rings and positions read left to right, as on the machine. Each piece of pipeline
input is a separate message, so Enigma starts each one from the same rotor positions.

## Examples

```powershell
'Attack at dawn' | Protect-CipherText -Cipher Caesar -Settings @{ RightShift = 3 }

$enigma = @{ Reflector = 'B'; Rotors = 'I', 'II', 'III'; Rings = 1, 1, 1; Positions = 'AAA'; Plugboard = 'AB CD' }
Protect-CipherText -Cipher Enigma -Settings $enigma -Text 'HELLO WORLD'

$settings = New-CipherSettings -Cipher Vigenere -Random
'Secret' | Protect-CipherText Vigenere $settings | Unprotect-CipherText Vigenere $settings

'YMJHFJXFWHNUMJWNXTSJTKYMJJFWQNJXYPSTBSFSIXNRUQJXYHNUMJWX' | Find-CipherKey -Cipher Caesar
Get-Content ciphertext.txt -Raw | Find-CipherKey -Cipher Homophonic -Language Italian
```

## Build and test

```powershell
./build.ps1
Invoke-Pester
```

`build.ps1` writes the module to `publish/SharpKind.Cryptography` at the repository root. The tests
need Pester 5.5 or later.
