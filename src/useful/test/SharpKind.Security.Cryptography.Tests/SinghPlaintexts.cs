// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// The plaintexts of Simon Singh's 'The Code Book' cipher challenge, shared by the cipher and stream tests.
/// </summary>
internal static class SinghPlaintexts
{
    /// <summary>
    /// Gets the stage 1 plaintext.
    /// </summary>
    internal const string Stage1 =
        "IN THE SAME HOUR CAME FORTH FINGERS OF A MAN'S HAND, " +
            "AND WROTE OVER AGAINST THE CANDLESTICK UPON THE PLASTER OF THE WALL OF THE KING'S PALACE; " +
            "AND THE KING SAW THE PART OF THE HAND THAT WROTE. " +
            "THEN THE KING'S COUNTENANCE WAS CHANGED, AND HIS THOUGHTS TROUBLED HIM, " +
            "SO THAT THE JOINTS OF HIS LOINS WERE LOOSED, AND HIS KNEES SMOTE ONE AGAINST ANOTHER. " +
            "THE KING CRIED ALOUD TO BRING IN THE ASTROLOGERS, THE CHALDEANS, AND THE SOOTHSAYERS. " +
            "AND THE KING SPAKE, AND SAID TO THE WISE MEN OF BABYLON, " +
            "WHOSOEVER SHALL READ THIS WRITING, AND SHOW ME THE INTERPRETATION THEREOF, " +
            "SHALL BE CLOTHED WITH SCARLET, AND HAVE A CHAIN OF GOLD ABOUT HIS NECK, " +
            "AND SHALL BE THE THIRD RULER IN THE KINGDOM. " +
            "THEN CAME IN ALL THE KING'S WISE MEN; " +
            "BUT THEY COULD NOT READ THE WRITING, NOR MAKE KNOWN TO THE KING THE INTERPRETATION THEREOF. " +
            "THEN WAS KING BELSHAZZAR GREATLY TROUBLED, AND HIS COUNTENANCE WAS CHANGED IN HIM, AND HIS LORDS WERE ASTONISHED. " +
            "NOW THE QUEEN, BY REASON OF THE WORDS OF THE KING AND HIS LORDS, CAME INTO THE BANQUET HOUSE; " +
            "AND THE QUEEN SPAKE AND SAID, O KING, LIVE FOREVER; " +
            "LET NOT THY THOUGHTS TROUBLE THEE, NOR LET THY COUNTENANCE BE CHANGED; " +
            "THERE IS A MAN IN THY KINGDOM, IN WHOM IS THE SPIRIT OF THE HOLY GODS; " +
            "AND IN THE DAYS OF THY FATHER LIGHT AND UNDERSTANDING AND WISDOM, " +
            "LIKE THE WISDOM OF THE GODS, WAS FOUND IN HIM; " +
            "WHOM THE KING NEBUCHADNEZZAR THY FATHER, THE KING, I SAY, THY FATHER, " +
            "MADE MASTER OF THE MAGICIANS, ASTROLOGERS, CHALDEANS, AND SOOTHSAYERS; " +
            "FORASMUCH AS AN EXCELLENT SPIRIT, AND KNOWLEDGE, AND UNDERSTANDING, " +
            "INTERPRETING OF DREAMS, AND SHOWING OF HARD SENTENCES, AND DISSOLVING OF DOUBTS, " +
            "WERE FOUND IN THE SAME DANIEL, WHOM THE KING NAMED BELTESHAZZAR; " +
            "NOW LET DANIEL BE CALLED, AND HE WILL SHOW THE INTERPRETATION. " +
            "THE FIRST CODEWORD IS OTHELLO. ";

    /// <summary>
    /// Gets the stage 2 plaintext.
    /// </summary>
    internal const string Stage2 =
        "FABER EST SUAE QUISQUE FORTUNAE APPIUS CLAUDIUS CAECUS DICTUM ARCANUM EST NEUTRON";

    /// <summary>
    /// Gets the stage 3 plaintext: Dante, Inferno XXVI, without punctuation, then the codeword.
    /// </summary>
    internal const string Stage3 =
        "O FRATI DISSI CHE PER CENTO MILIA PERIGLI SIETE GIUNTI A LOCCIDENTE A QUESTA TANTO " +
            "PICCIOLA VIGILIA DE NOSTRI SENSI CHE DEL RIMANENTE NON VOGLIATE NEGAR LESPERIENZA DIRETRO " +
            "AL SOL DEL MONDO SENZA GENTE CONSIDERATE LA VOSTRA SEMENZA FATI NON FOSTE A VIVER COME " +
            "BRUTI MA PER SEGUIR VIRTUTE E CANOSCENZA LI MIEI COMPAGNI FECIO SI AGUTI CON QUESTA " +
            "ORAZION PICCIOLA AL CAMMINO CHE A PENA POSCIA LI AVREI RITENUTI E VOLTA NOSTRA POPPA NEL " +
            "MATTINO DEI REMI FACEMMO ALI AL FOLLE VOLO SEMPRE ACQUISTANDO DAL LATO MANCINO TUTTE LE " +
            "STELLE GIA DE LALTRO POLO VEDEA LA NOTTE E L NOSTRO TANTO BASSO CHE NON SURGEA FUOR DEL " +
            "MARIN SUOLO CINQUE VOLTE RACCESO E TANTO CASSO LO LUME ERA DI SOTTO DA LA LUNA POI CHE " +
            "NTRATI ERAVAM NE LALTO PASSOQUANDO NAPPARVE UNA MONTAGNA BRUNA PER LA DISTANZA E PARVEMI " +
            "ALTA TANTO QUANTO VEDUTA NON AVEA ALCUNA NOI CI ALLEGRAMMO E TOSTO TORNO IN PIANTO CHE DE " +
            "LA NOVA TERRA UN TURBO NACQUE E PERCOSSE DEL LEGNO IL PRIMO CANTO TRE VOLTE IL FE GIRAR " +
            "CON TUTTE LACQUE A LA QUARTA LEVAR LA POPPA IN SUSO E LA PRORA IRE IN GIU COMALTRUI " +
            "PIACQUEINFIN CHE L MAR FU SOPRA NOI RICHIUSO  LA PAROLA IN CODICE E EQUATOR";

    /// <summary>
    /// Gets the stage 4 plaintext.
    /// </summary>
    internal const string Stage4 =
        "SOUVENT POUR SAMUSER LES HOMMES DEQUIPAGE " +
            "PRENNENT DES ALBATROS VASTES OISEAUX DES MERS " +
            "QUI SUIVENT INDOLENTS COMPAGNONS DE VOYAGE " +
            "LE NAVIRE GLISSANT SUR LES GOUFFRES AMERS " +
            "A PEINE LES ONTILS DEPOSES SUR LES PLANCHES " +
            "QUE CES ROIS DE LAZUR MALADROITS ET HONTEUX " +
            "LAISSENT PITEUSEMENT LEURS GRANDES AILES BLANCHES " +
            "COMME DES AVIRONS TRAINER A COTE DEUX " +
            "CE VOYAGEUR AILE COMME IL EST GAUCHE ET VEULE " +
            "LUI NAGUERE SI BEAU QUIL EST COMIQUE ET LAID " +
            "LUN AGACE SON BEC AVEC UN BRULE GUEULE " +
            "LAUTRE MIME EN BOITANT LINFIRME QUI VOLAIT " +
            "LE POETE EST SEMBLABLE AU PRINCE DES NUEES " +
            "QUI HANTE LA TEMPETE ET SE RIT DE LARCHER " +
            "BAUDELAIRE " +
            "EXILE SUR LE SOL AU MILIEU DES HUEES " +
            "LE MOT POUR ETAGE QUATRE EST TRAJAN " +
            "SES AILES DEGEANT LEMPECHENT DE MARCHER";

    /// <summary>
    /// Gets the stage 5 plaintext.
    /// </summary>
    internal const string Stage5 =
        "PLAIFAIRCIPHERESELPROXIMONIVELLAPALABRASECRETAESILLIAD";

    /// <summary>
    /// Gets the stage 6 plaintext.
    /// </summary>
    internal const string Stage6 =
        "THISSTAGEISNUMBERSIXANDITISAPLAYFAIRCIPHERSTOPTHEFOLLOWI" +
            "NGSTAGEISNUMBERSEVENWITHINTHECONTEXTOFTHISCOMPETITIONAND" +
            "ITISENCRYPTEDACXCORDINGTOANADFGVXTYPECIPHERSTOPWHENYOUAT" +
            "TEMPTXTOCRYPTANALYSESTAGESEVENYOUWILLSEXETHATITISMORECOM" +
            "PLICATEDTHANASTRAIGHTFORWARDADFGVXCIPHERSTOPUNFORTUNATEL" +
            "YICANXNOTGIVEYOUANYMORECLUESSTOPNEWPARAGRAPHINTERMSOFTHI" +
            "SXSTAGETHESHIBBOLETHTHATYOUSHOULDTAKENOTEOFISMOLYBDENUMN" +
            "EWPARAGRAPHYOUWILXLPROBABLYHAVENOTICEDBYNOWTHATEACHSTAGE" +
            "ISINADIFFERENTCIPHERANDISUSUALLYINANAPPROPRIATELANGUAGES" +
            "TOPSOTHEVIGENERESTAGEWASINFRENCHANDTHISPLAYFAIRSTAGEISIN" +
            "ENGLISHSTOPIFTHEPATXTERNPERSISTSTHENTHENEXTSTAGEWILXLBEI" +
            "NGERMANSTOPBUTDOESTHEPATTERNPERSISTQUESTIONMARKQ";

    /// <summary>
    /// Gets the stage 8 plaintext.
    /// </summary>
    internal const string Stage8 =
        "DASXL OESUN GSWOR TXIST XPLUT" +
            "OXXST UFEXN EUNXE NTHAE LTXEI" +
            "NEXMI TTEIL UNGXD IEXMI TXDES" +
            "XENTK ODIER TXIST XXICH XHABE" +
            "XDASX LINKS STEHE NDEXB YTEXD" +
            "ESXSC HLUES SELSX ENTDE CKTXX" +
            "ESXIS TXEIN SXEIN SXZER OXEIN" +
            "SXZER OXZER OXEIN SXEIN SXEIN" +
            "SXXIC HXPRO GRAMM IERTE XDESX" +
            "UNDXE NTDEC KTEXD ASSXD ASXWO" +
            "RTXDE BUGGE RXWEN NXESX MITXD" +
            "EMXUN TENST EHEND ENXSC HLUES" +
            "SELXE NTKOD IERTX WIRDX ALSXR" +
            "ESULT ATXDI EXUNT ENSTE HENDE" +
            "NXSCH RIFTZ EICHE NXHAT";

    /// <summary>
    /// Gets the stage 7 plaintext: a German passage about Fermat, then the codeword and the Enigma note.
    /// Punctuation other than the full stop is gone, and the umlauts are plain vowels.
    /// </summary>
    internal const string Stage7 =
        "Pierre de Fermat wurde am 20.August 1601 in der sudwestfranzoischen Stadt Beaumont de " +
            "Lomagne geboren.Der Vater Dominique Fermat war ein wohlhabender Lederhandler und so kam " +
            "Fermat in den Genuss einer einer vorzuglichen Schulbildung im Franziskanerkloster " +
            "Grandselve der sich ein Studium an der Universitat von Toulouse anschloss.Ob sich Fermat " +
            "schon als junger Mann in der Mathematik besonders hervorgetan hat ist nicht bekannt.Auf " +
            "Drangen der Familie schlug Fermat eine juristische Laufbahn ein und wurde 1631 zum " +
            "Conseiller au Parlement de Toulouse ernannt wo er als Hofrat an der Petitionskammer tatig " +
            "war.Wollten die Burger der Stadt ein Gesuch an den Konig richten mussten sie zuallererst " +
            "Fermat oder einen seiner Amtskollegen vom Gewicht ihrer Eingabe uberzeugen.Die Rate waren " +
            "die Lebensader zwischen Paris und der Provinz.Sie vermittelten jedoch nicht nur zwischen " +
            "der lokalen Bevolkerung und dem Monarchen sondern gewahrleisteten auch dass die " +
            "koniglichen Erlasse aus der Hauptstadt in den Regionen befolgt wurden.Fermat war ein " +
            "tuchtiger Staatsdiener der nach allem was man weiss seine Pflichten gewissenhaft und " +
            "umsichtig erfullte.Fermat war ausserdem als Richter tatig und kraft seines hohen Ranges " +
            "mit den schwersten Fallen betraut.Der englische Mathematiker Sir Kenelm Digby berichtet " +
            "von dieser Arbeit.Digby hatte Fermat um ein Treffen ersucht doch in einem Brief an den " +
            "gemeinsamen Kollegen John Wallis schrieb er der Franzose sei mit dringenden " +
            "Justizangelegenheiten befasst und habe ihn deshalb nicht empfangen konnen.Nun hatte ich " +
            "leider genau den Tag erwischt da die Versetzung der Richter von Castres nach Toulouse " +
            "stattfand wo er Fermat zum leitenden Richter am obersten Gerichtshof des Parlaments " +
            "bestallt wurde.Seither ist er mit Kapitalverbrechen von grosser Bedeutung befasst.In einem " +
            "Fall hat er ein Urteil gesprochen welches grosses Aufsehen erregt hat.Es ging um die " +
            "Verurteilung eines Priesters der sein Amt missbraucht hatte zum Tode auf dem " +
            "Scheiterhaufen.Diese Strafsache ist gerade abgeschlossen die Hinrichtung ist " +
            "vollzogen.Fermat korrespondierte regelmassig mit Digby und Wallis.Seine Briefe waren wie " +
            "wir noch sehen werden oft nicht gerade freundlich doch bieten interessante Einblicke in " +
            "Fermats Alltag und seine wissenschaftliche Tatigkeit.Fermat machte im Staatsdienst rasch " +
            "Karriere gewann Zugang zu den fuhrenden gesellschaftlichen Kreisen und auch das Recht ein " +
            "de im Namen zu fuhren.Den Aufstieg hatte er jedoch weniger seinem Ehrgeiz als seiner " +
            "robusten Gesundheit zu verdanken.In ganz Europa wutete damals die Pest und wer sie " +
            "uberlebte wurde ausersehen die Platze der Verstorbenen einzunehmen.Selbst Fermat erlitt im " +
            "Jahre 1652 einen Pestanfall und erkrankte so schwer dass sein Freund Bernard Medon " +
            "mehreren Kollegen mitteilte er sei tot.Kurze Zeit spater jedoch korrigierte er sich in " +
            "einem Brief an den Hollander Nicholas Heinsius.Ich habe Ihnen vor einiger Zeit mitgeteilt " +
            "Fermat sei verstorben.Doch er lebt und wir furchten nun nicht mehr um seine Gesundheit " +
            "auch wenn wir ihn vor kurzem noch zu den Toten zahlten.ENDE.Das gesuchte Wort ist " +
            "Edison.ENDE.Der naechste Teil enthaelt eine von einer Enigmamaschine verschluesselte " +
            "Mitteilung.Die Abbildung zeigt die Verbindungen innerhalb jeder Walze und der Umkehrwalze " +
            "der Maschine.Es ist dennoch zu beachten dass das Walzenlage falsch wiedergegeben " +
            "ist.Eigentlich sollen Walze 1 und Walze 2 gegeneinander ausgetauscht werden.Die " +
            "urspruengliche Walzestellung und Ringstellung und Steckerverbindungenstellung sind " +
            "unbekannt.";

    /// <summary>
    /// Gets the stage 9 plaintext. The last 8 byte block of the ciphertext does not decrypt to text, so it is not included.
    /// </summary>
    internal const string Stage9 = """""
O! cursed, cursed slave.  Whip me, ye devils,
From the possession of this heavenly sight!
Blow me about in winds! roast me in sulphur!
Wash me in steep-down gulfs of liquid fire!
O Desdemona! Desdemona! dead!

Only one more phase to go.  Your token for this one is "Armstrong".

The final hurdle contains two ciphertexts.  The longer one is
encrypted with triple-DES in electronic code book mode.  It uses a
128-bit EDE key.

The RSA public key cryptosystem has been used to encipher the
triple-DES key; this forms the shorter ciphertext.  The public
exponent is 3735928559 in decimal and the public modulus is the
following 155-digit decimal integer:

10742 78829 12665 65907 17841 12799 42116 61266 39217 94753 29458
88778 17210 35546 41509 80121 87903 38329 26235 28109 07506 72083
50494 19964 33143 42555 83344 01855 80898 94268 92463.

Your task is to decipher the longer message and so discover the final
code word.  How you do this is up to you, but it will almost certainly
be much, much easier to break the RSA ciphertext by factoring the
public modulus than by any other approach.

Good luck!


""""";

    /// <summary>
    /// Gets the stage 10 plaintext. The last 2 bytes of the ciphertext do not decrypt to text, so they are not included.
    /// </summary>
    internal const string Stage10 = """""
For secrets are edged tools and must be kept from children and from fools.

The magic word you need to quote is DRYDEN.

Congratulations, you have solved the tenth and final stage of the challenge.

""""";
}
