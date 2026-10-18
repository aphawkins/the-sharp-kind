# Simon Singh's Cipher Challenge

Simon Singh's *The Code Book* (1999) ends with ten ciphertexts, one for each stage of a competition. Every stage
uses a different cipher, and solving a stage reveals a codeword and often a clue to the next stage. The challenge
was won on 5 October 2000. This page records what is known about each stage, and how the SharpKind cryptography
libraries cover it.

## Sources

- [singh-corrected-ciphers.txt](singh-corrected-ciphers.txt) - the ten ciphertexts, as collected from the book.
  Jim Gillogly relays that Singh gave permission to put the ciphertexts on the web, provided they are credited to
  *The Code Book* by Simon Singh. A later editor (simon@outflux.net) added English subtitles to stages 1 to 4 and
  corrected formatting errors in stages 8 and 10.
- [singh-how-we-cracked-the-code-book-ciphers.txt](singh-how-we-cracked-the-code-book-ciphers.txt) - Almgren,
  Andersson, Granlund, Ivansson and Ulfberg, *How we Cracked the Code Book Ciphers* (11 October 2000). It is the
  source of nearly every key below.
- [dante-inferno-xxvi.txt](dante-inferno-xxvi.txt) - Dante, *Inferno* XXVI, from Project Gutenberg. It is the
  plaintext of stage 3.

Editions of the book differ. The stage 1 plaintext was translated into the language of each edition (the Swedish
edition has Swedish), so the English ciphertext here is the English edition's.

## The stages

| Stage | Cipher | Plaintext | Codeword | Status here |
|---|---|---|---|---|
| 1 | Monoalphabetic substitution | English: Daniel 5, Belshazzar's feast | OTHELLO | Solved and cracked: `MonoAlphabetic`, `MonoAlphabeticCryptanalysis` |
| 2 | Caesar shift (7) | Latin: Appius Claudius Caecus | NEUTRON | Solved: `Caesar` |
| 3 | Homophonic substitution | Italian: Dante, Inferno XXVI | EQUATOR | Solved: `Homophonic` |
| 4 | Vigenere (keyword SCUBA) | French: Baudelaire, *L'Albatros* | TRAJAN | Solved: `Vigenere` |
| 5 | Book cipher | Spanish | ILLIAD | Solved: `BookCipher` |
| 6 | Playfair | English | MOLYBDENUM | Solved: `Playfair` |
| 7 | ADFGVX-style (8 by 8) | German: Fermat's biography, from *Fermat's Last Theorem* | EDISON | Solved: `Adfgvx` |
| 8 | Enigma | German | PLUTO | Solved: `Enigma` |
| 9 | DES, ECB mode | English: Othello, Act V | ARMSTRONG | Solved: .NET `DES` |
| 10 | RSA wrapping a DES key | English: Dryden | DRYDEN | Solved: .NET `DES`, `BigInteger` |

The tests are in `SinghCipherChallengeTests` (`SharpKind.Security.Cryptography.Tests`) and
`SinghCipherChallengeSymmetricTests` (`SharpKind.Security.Cryptography.Streams.Tests`). The ciphertexts, plaintexts
and shared keys they use are in `SinghCiphertexts`, `SinghPlaintexts` and `SinghKeys`. The stream tests link those
files rather than copying them. Stages 9 and 10 use .NET's DES and BigInteger, so they have no stream test.

Most plaintexts are upper case and have no punctuation, because the classical ciphers drop it. Singh's stage 3 and
stage 8 plaintexts are written without punctuation too. The stage 7 plaintext keeps its case and its full stops.

### Stage 1: monoalphabetic substitution

The book says it is a substitution cipher, and the word breaks are kept, so short common words give it away. The
plaintext is the book of Daniel, chapter 5, in the King James wording, ending "The first codeword is Othello".
`MonoAlphabeticCryptanalysisTests` cracks it with no key. `MonoAlphabeticCryptanalysis` hill-climbs over keys and
scores each decryption by English quadgram statistics (from practicalcryptography.com).

### Stage 2: Caesar shift

Shift 7. The plaintext is *Faber est suae quisque fortunae* (every man is the maker of his own fortune), attributed
to Appius Claudius Caecus, then *dictum arcanum est neutron*. The cryptanalysis test in
`CaesarCryptanalysisTests` (`SharpKind.Security.Cryptography.Cryptanalysis.Tests`) finds the shift by letter frequencies.

### Stage 3: homophonic substitution (Dante)

This stage is the one with the least documentation, so the key here comes from lining the ciphertext up against
Dante's text, and from the paper's clue that several letters stand for the same plaintext letter. The plaintext is
the speech of Ulysses in *Inferno* XXVI, verses 112 to 142, starting "O frati, dissi, che per cento milia
perigli siete giunti a l'occidente". After the last verse, `XX` and the words "la parola in codice e EQUATOR" are
the codeword.

Singh's text has no punctuation and no accents, apostrophes close up ("loccidente"), and *è* is written *e*. It
differs from Project Gutenberg's edition in small ways, such as *de nostri sensi* where Gutenberg has *d'i nostri
sensi*, so lining the two up needs a fuzzy match.

The ciphertext is 1,122 characters on 21 lines, with `*` as a symbol. The key:

| Plaintext | Symbols |
|---|---|
| space | `X` |
| A | `M` `B` `C` `G` `N` `Z` (six homophones: each is used for a run of lines) |
| B | `A` |
| C | `S` |
| D | `L` |
| E | `Q` |
| F | `D` |
| G | `J` |
| H | `O` |
| I | `F` |
| L | `*` |
| M | `W` |
| N | `T` |
| O | `I` |
| P | `Y` |
| Q | `R` |
| R | `V` |
| S | `E` |
| T | `U` |
| U | `P` |
| V | `K` |
| Z | `H` |

J, K, W, X and Y never occur in the Italian text. Notice that the cipher letters `A` and `H` stand for plaintext B
and Z.

The solvers lost hours assuming that the cipher was diabolical, with digrams and "magic" characters. The way in
was to notice that M, G, C, Z, N and B each appear only on a few lines but are very frequent there, which showed
that they were one letter, and the most common one, at about 10%. They then tried Italian, found the first words
in a search engine, and named Dante.

`Homophonic` decrypts this exactly. It cannot reproduce Singh's ciphertext by encrypting, because it takes the
homophones for A in turn, one for each use, where Singh used one for a whole run of lines.

### Stage 4: Vigenere

The book names the cipher. Three of the solvers had written a Vigenere solver for a cryptography course, and it
solved the stage in a few minutes. Its index of coincidence scores pointed to a keyword length of 5 (and its
multiple, 10). The keyword is SCUBA. The plaintext is the French of Baudelaire's poem *L'Albatros*, and it ends
with the codeword: "le mot pour etage quatre est Trajan".

### Stage 5: book cipher

The solvers' ninth stage, found on 15 May 2000, six months after they began. They believed from the start that it
was a book cipher, with each number an index into a key text, but they first tried words and the wrong books. There
are 54 numbers, up to 217. The crossword in the book (15 by 15 squares) and the Rosetta Stone (54 verses) were among
the red herrings.

The key text is Fermat's marginal note, in Latin: *Cubum autem in duos cubos, aut quadratoquadratum in duos
quadratoquadratos, et generaliter nullam in infinitum ultra quadratum potestatem in duos eiusdem nominis fas est
dividere. Cuius rei demonstrationem mirabilem sane detexi. Hanc marginis exiguitas non caperet.* Counting
letters only, with the first letter as number 1, the numbers spell out
`PLAIFAIRCIPHERESELPROXIMONIVELLAPALABRASECRETAESILLIAD`: "Playfair cipher es el proximo nivel. La palabra
secreta es Illiad." (The Spanish is Singh's.) The text must keep "infinitum" in full: with the "fi" ligature
dropped, the letter count shifts and the result is nonsense.

`BookCipher` takes the key text, counts only its letters A to Z, and decrypts any index. Encrypting uses the first
occurrence of a letter, though Singh's numbers repeat 119 four times and use other occurrences of other letters.

### Stage 6: Playfair

Playfair was suspected from the ciphertext: there is no J, single letters have a smooth distribution while digrams
do not, and the order of the stages suggested it. The solvers searched for the key square with a heuristic search
that scored English digram log-probabilities, as filling the top of the square with English text did not work.

The key square is
```
N K E V I
R H C T O
G B A L U
P D M F S
Z Q W X Y
```
that is, the 25-letter keyword `NKEVIRHCTOGBALUPDMFSZQWXY`. Rotated one column left it reads `KEVIN HCTOR BALUG DMFSP
QWXYZ`, which looks like Kevin Hector and Brian Clough, of Derby County in the 1970s. The plaintext is English and
ends with a Q for padding. It says that stage 7 is an ADFGVX-type cipher, and that the codeword is molybdenum.

### Stage 7: ADFGVX-style

The ciphertext has 7,048 characters, which is 8 by 881, so the matrix is 8 columns wide. The labels are the 8 letters
`CEMOPRTU`, which makes a 64-character Polybius square, not the 36 of ADFGVX. The solvers guessed that the alphabet is
the 52 upper and lower case letters, the 10 digits, a space and a full stop, and that guess was right. They had feared
a harder cipher than ADFGVX, but the plain method worked. The steps to decrypt are:

1. Write the ciphertext into an 8 by 881 matrix.
2. Permute the columns according to the (8 letter) keyword.
3. Group neighbouring columns into pairs.
4. Substitute each pair using the square.

The permutation was found by displaying the digram distribution of every pair of columns. Four column pairs gave
24 possible orders, and the right one was the one that gave German letter frequencies for the first four characters.
The plaintext is German, which the solvers recognized as a passage of Singh's *Fermat's Last Theorem*. Near the end it
holds the codeword EDISON (which is not the transposition keyword) and the clue that the next part is an Enigma
message, with the wheels shown in the wrong order: wheel 1 and wheel 2 must be swapped.

Neither the square nor the keyword was published, and the solvers' notes do not give them. The key in
`SinghKeys` was recovered from the German edition of the book (dtv), which has the same passage:

- **Columns.** The index of coincidence of each ordered pair of columns shows the four pairs: ciphertext columns
  (1,2), (3,4), (5,6) and (7,0), when each column is 881 characters read down the ciphertext. Joined in that order,
  the matrix column that each ciphertext chunk belongs to is 7, 0, 1, 2, 3, 4, 5, 6. Any keyword whose letters sort in
  that order does the same job; `BCDEFGHA` is used. Singh's own keyword is not known.
- **Square.** The decrypted text starts "Pierre de Fermat wurde am 20. August 1601 in der..." The passage is in the
  chapter on Fermat ("Der Fuerst der Amateure"). Lining it up with the book gives the character for each of the 56
  digraphs that occur. Eight cells of the square are in no digraph; the characters `Q X Y x 4 7 8 9` are the ones left
  over, put in those cells in order, which is a guess and affects nothing. The row and column labels are numbered in
  the order of `CEMOPRTU`; Singh's own order is not known, and the numbering is only a name.

The plaintext is the book's text with the umlauts written as plain vowels, with `ss` for the eszett, and with all
punctuation gone except the full stop, which has no space after it ("geboren.Der Vater"). It ends with
`ENDE Das gesuchte Wort ist Edison ENDE`, then the Enigma note. It contains oddities that look like the published
ciphertext's own: "sudwestfranzoischen" and "einer einer". With the key, `Adfgvx` both decrypts the ciphertext to this
text and encrypts the text back to exactly the same ciphertext.

`Adfgvx` is case sensitive in its square for this, since one square holds both cases of the letters. When a character
is not found as it is, it is looked up in upper case.

### Stage 8: Enigma

Three wheels, a reflector and a plugboard, all in German. The wheels in the figure were the wrong way round (see
stage 7). The settings used by the tests:

- Reflector B.
- Wheel order III, II, I.
- Ring positions 01, 06, 01.
- Plugboard pairs AS, EI, JN, KL, MU, OT.
- Message key ALL (so the final key is AAR).

The solvers attacked the plugboard first, as it changes only 12 letters. They tried all 17,576 wheel and ring
settings, finding the best plugboard for each by solving a matching problem on a text of all E, and scored the
results. German letter frequencies failed, because the plaintext uses X for a space and XX as a delimiter. Four of
the six plugboard pairs came out right, and the last two were fixed by hand.

The plaintext reads: "Das Loesungswort ist PLUTO. Stufe neun enthaelt eine Mitteilung die mit DES entkodiert ist.
Ich habe das linksstehende Byte des Schluessels entdeckt. Es ist eins eins zero eins zero zero eins eins eins."
The second part says that the word DEBUGGER, decrypted with the key printed in stage 8, gives the printed letters,
which lets you check a DES implementation.

### Stage 9: DES

The stage 8 clue gives the first byte of the 8 byte key (the bits 11010011, which is `d3`), leaving 2^48 keys,
because the eighth bit of each key byte is a parity bit. The solvers assumed ECB mode and tested whether blocks
decrypted to seven-bit ASCII. A bit-sliced search tried over 3 million keys a second on the fastest workstation, and a
server handed out jobs of 2^30 keys to Unix clients and Windows screen savers. They found the key three weeks later,
after searching about 17% of the key space, on a slow PC whose owner arrived late for work that day.

The key is `d3cd1694cba126fe`. The plaintext is Othello, Act V, ("O cursed, cursed slave...") followed by
instructions: the final stage has two ciphertexts, the longer one is enciphered with triple DES in ECB mode with a
128-bit EDE key, and the shorter is that key enciphered with RSA, with public exponent 3,735,928,559 (0xDEADBEEF) and
a 155-digit modulus. The codeword is ARMSTRONG. The scan of the book had introduced OCR errors, so the stage 9
ciphertext in this repository is the corrected one.

The last block of the ciphertext does not decrypt to text. `SinghPlaintexts.Stage9` leaves it out.

### Stage 10: RSA and DES

The 155-digit modulus has no small factors, so only the general number field sieve would do. One of the solvers
estimated about 40 CPU years. They borrowed the code that CWI had used for the record factorization of RSA-155, ran
the sieving on hundreds of machines, and filtered 75 million relations down to a system of 8.4 million equations.
Solving that took 13 days on a quad processor Compaq Alpha ES40. The factors, of 78 and 77 digits, were found on
5 October 2000.

algorithm and decrypts the short message to a 128-bit value, `462c6bad381031ad64dccd9820a751a2`. The solvers
first tried triple DES, as Singh's stage 9 plaintext says, and got random text. The longer message is single DES,
ECB mode, with the first half, `462c6bad381031ad`, as the key. They saw this when one of them spotted that a
temporary file of their DES program already held the plaintext. It is Dryden's couplet "For secrets are edged
tools, and must be kept from children and from fools", and the codeword DRYDEN.

The last 2 bytes of the ciphertext do not decrypt to text.

## What is not known

- Singh's own stage 7 keyword and square layout. The key here is the one that reproduces the ciphertext, not necessarily his.
- The exact choice of homophone for each A in stage 3, which varies by line.
- Singh's rule for choosing between the six stage 3 symbols for A. The key above lists the symbols only.
