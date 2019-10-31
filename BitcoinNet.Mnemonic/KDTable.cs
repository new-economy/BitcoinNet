﻿using System;
using System.Linq;
using System.Text;

namespace BitcoinNet.Mnemonic
{
	public class KDTable
	{
		private const string SubstitutionTable =
			"  \n¨ ̈\nªa\n¯ ̄\n²2\n³3\n´ ́\nµμ\n¸ ̧\n¹1\nºo\n¼1⁄4\n½1⁄2\n¾3⁄4\nÀÀ\nÁÁ\nÂÂ\nÃÃ\nÄÄ\nÅÅ\nÇÇ\nÈÈ\nÉÉ\nÊÊ\nËË\nÌÌ\nÍÍ\nÎÎ\nÏÏ\nÑÑ\nÒÒ\nÓÓ\nÔÔ\nÕÕ\nÖÖ\nÙÙ\nÚÚ\nÛÛ\nÜÜ\nÝÝ\nàà\náá\nââ\nãã\nää\nåå\nçç\nèè\néé\nêê\nëë\nìì\níí\nîî\nïï\nññ\nòò\nóó\nôô\nõõ\nöö\nùù\núú\nûû\nüü\nýý\nÿÿ\nĀĀ\nāā\nĂĂ\năă\nĄĄ\nąą\nĆĆ\nćć\nĈĈ\nĉĉ\nĊĊ\nċċ\nČČ\nčč\nĎĎ\nďď\nĒĒ\nēē\nĔĔ\nĕĕ\nĖĖ\nėė\nĘĘ\nęę\nĚĚ\něě\nĜĜ\nĝĝ\nĞĞ\nğğ\nĠĠ\nġġ\nĢĢ\nģģ\nĤĤ\nĥĥ\nĨĨ\nĩĩ\nĪĪ\nīī\nĬĬ\nĭĭ\nĮĮ\nįį\nİİ\nĲIJ\nĳij\nĴĴ\nĵĵ\nĶĶ\nķķ\nĹĹ\nĺĺ\nĻĻ\nļļ\nĽĽ\nľľ\nĿL·\nŀl·\nŃŃ\nńń\nŅŅ\nņņ\nŇŇ\nňň\nŉʼn\nŌŌ\nōō\nŎŎ\nŏŏ\nŐŐ\nőő\nŔŔ\nŕŕ\nŖŖ\nŗŗ\nŘŘ\nřř\nŚŚ\nśś\nŜŜ\nŝŝ\nŞŞ\nşş\nŠŠ\nšš\nŢŢ\nţţ\nŤŤ\nťť\nŨŨ\nũũ\nŪŪ\nūū\nŬŬ\nŭŭ\nŮŮ\nůů\nŰŰ\nűű\nŲŲ\nųų\nŴŴ\nŵŵ\nŶŶ\nŷŷ\nŸŸ\nŹŹ\nźź\nŻŻ\nżż\nŽŽ\nžž\nſs\nƠƠ\nơơ\nƯƯ\nưư\nǄDŽ\nǅDž\nǆdž\nǇLJ\nǈLj\nǉlj\nǊNJ\nǋNj\nǌnj\nǍǍ\nǎǎ\nǏǏ\nǐǐ\nǑǑ\nǒǒ\nǓǓ\nǔǔ\nǕǕ\nǖǖ\nǗǗ\nǘǘ\nǙǙ\nǚǚ\nǛǛ\nǜǜ\nǞǞ\nǟǟ\nǠǠ\nǡǡ\nǢǢ\nǣǣ\nǦǦ\nǧǧ\nǨǨ\nǩǩ\nǪǪ\nǫǫ\nǬǬ\nǭǭ\nǮǮ\nǯǯ\nǰǰ\nǱDZ\nǲDz\nǳdz\nǴǴ\nǵǵ\nǸǸ\nǹǹ\nǺǺ\nǻǻ\nǼǼ\nǽǽ\nǾǾ\nǿǿ\nȀȀ\nȁȁ\nȂȂ\nȃȃ\nȄȄ\nȅȅ\nȆȆ\nȇȇ\nȈȈ\nȉȉ\nȊȊ\nȋȋ\nȌȌ\nȍȍ\nȎȎ\nȏȏ\nȐȐ\nȑȑ\nȒȒ\nȓȓ\nȔȔ\nȕȕ\nȖȖ\nȗȗ\nȘȘ\nșș\nȚȚ\nțț\nȞȞ\nȟȟ\nȦȦ\nȧȧ\nȨȨ\nȩȩ\nȪȪ\nȫȫ\nȬȬ\nȭȭ\nȮȮ\nȯȯ\nȰȰ\nȱȱ\nȲȲ\nȳȳ\nʰh\nʱɦ\nʲj\nʳr\nʴɹ\nʵɻ\nʶʁ\nʷw\nʸy\n˘ ̆\n˙ ̇\n˚ ̊\n˛ ̨\n˜ ̃\n˝ ̋\nˠɣ\nˡl\nˢs\nˣx\nˤʕ\ǹ̀\ń́\n̓̓\n̈́̈́\nʹʹ\nͺ ͅ\n;;\n΄ ́\n΅ ̈́\nΆΆ\n··\nΈΈ\nΉΉ\nΊΊ\nΌΌ\nΎΎ\nΏΏ\nΐΐ\nΪΪ\nΫΫ\nάά\nέέ\nήή\nίί\nΰΰ\nϊϊ\nϋϋ\nόό\nύύ\nώώ\nϐβ\nϑθ\nϒΥ\nϓΎ\nϔΫ\nϕφ\nϖπ\nϰκ\nϱρ\nϲς\nϴΘ\nϵε\nϹΣ\nЀЀ\nЁЁ\nЃЃ\nЇЇ\nЌЌ\nЍЍ\nЎЎ\nЙЙ\nйй\nѐѐ\nёё\nѓѓ\nїї\nќќ\nѝѝ\nўў\nѶѶ\nѷѷ\nӁӁ\nӂӂ\nӐӐ\nӑӑ\nӒӒ\nӓӓ\nӖӖ\nӗӗ\nӚӚ\nӛӛ\nӜӜ\nӝӝ\nӞӞ\nӟӟ\nӢӢ\nӣӣ\nӤӤ\nӥӥ\nӦӦ\nӧӧ\nӪӪ\nӫӫ\nӬӬ\nӭӭ\nӮӮ\nӯӯ\nӰӰ\nӱӱ\nӲӲ\nӳӳ\nӴӴ\nӵӵ\nӸӸ\nӹӹ\nևեւ\nآآ\nأأ\nؤؤ\nإإ\nئئ\nٵاٴ\nٶوٴ\nٷۇٴ\nٸيٴ\nۀۀ\nۂۂ\nۓۓ\nऩऩ\nऱऱ\nऴऴ\nक़क़\nख़ख़\nग़ग़\nज़ज़\nड़ड़\nढ़ढ़\nफ़फ़\nय़य़\nোো\nৌৌ\nড়ড়\nঢ়ঢ়\nয়য়\nਲ਼ਲ਼\nਸ਼ਸ਼\nਖ਼ਖ਼\nਗ਼ਗ਼\nਜ਼ਜ਼\nਫ਼ਫ਼\nୈୈ\nୋୋ\nୌୌ\nଡ଼ଡ଼\nଢ଼ଢ଼\nஔஔ\nொொ\nோோ\nௌௌ\nైై\nೀೀ\nೇೇ\nೈೈ\nೊೊ\nೋೋ\nൊൊ\nോോ\nൌൌ\nේේ\nොො\nෝෝ\nෞෞ\nำํา\nຳໍາ\nໜຫນ\nໝຫມ\n༌་\nགྷགྷ\nཌྷཌྷ\nདྷདྷ\nབྷབྷ\nཛྷཛྷ\nཀྵཀྵ\nཱཱིི\nཱཱུུ\nྲྀྲྀ\nཷྲཱྀ\nླྀླྀ\nཹླཱྀ\nཱཱྀྀ\nྒྷྒྷ\nྜྷྜྷ\nྡྷྡྷ\nྦྷྦྷ\nྫྷྫྷ\nྐྵྐྵ\nဦဦ\nჼნ\nᬆᬆ\nᬈᬈ\nᬊᬊ\nᬌᬌ\nᬎᬎ\nᬒᬒ\nᬻᬻ\nᬽᬽ\nᭀᭀ\nᭁᭁ\nᭃᭃ\nᴬA\nᴭÆ\nᴮB\nᴰD\nᴱE\nᴲƎ\nᴳG\nᴴH\nᴵI\nᴶJ\nᴷK\nᴸL\nᴹM\nᴺN\nᴼO\nᴽȢ\nᴾP\nᴿR\nᵀT\nᵁU\nᵂW\nᵃa\nᵄɐ\nᵅɑ\nᵆᴂ\nᵇb\nᵈd\nᵉe\nᵊə\nᵋɛ\nᵌɜ\nᵍg\nᵏk\nᵐm\nᵑŋ\nᵒo\nᵓɔ\nᵔᴖ\nᵕᴗ\nᵖp\nᵗt\nᵘu\nᵙᴝ\nᵚɯ\nᵛv\nᵜᴥ\nᵝβ\nᵞγ\nᵟδ\nᵠφ\nᵡχ\nᵢi\nᵣr\nᵤu\nᵥv\nᵦβ\nᵧγ\nᵨρ\nᵩφ\nᵪχ\nᵸн\nᶛɒ\nᶜc\nᶝɕ\nᶞð\nᶟɜ\nᶠf\nᶡɟ\nᶢɡ\nᶣɥ\nᶤɨ\nᶥɩ\nᶦɪ\nᶧᵻ\nᶨʝ\nᶩɭ\nᶪᶅ\nᶫʟ\nᶬɱ\nᶭɰ\nᶮɲ\nᶯɳ\nᶰɴ\nᶱɵ\nᶲɸ\nᶳʂ\nᶴʃ\nᶵƫ\nᶶʉ\nᶷʊ\nᶸᴜ\nᶹʋ\nᶺʌ\nᶻz\nᶼʐ\nᶽʑ\nᶾʒ\nᶿθ\nḀḀ\nḁḁ\nḂḂ\nḃḃ\nḄḄ\nḅḅ\nḆḆ\nḇḇ\nḈḈ\nḉḉ\nḊḊ\nḋḋ\nḌḌ\nḍḍ\nḎḎ\nḏḏ\nḐḐ\nḑḑ\nḒḒ\nḓḓ\nḔḔ\nḕḕ\nḖḖ\nḗḗ\nḘḘ\nḙḙ\nḚḚ\nḛḛ\nḜḜ\nḝḝ\nḞḞ\nḟḟ\nḠḠ\nḡḡ\nḢḢ\nḣḣ\nḤḤ\nḥḥ\nḦḦ\nḧḧ\nḨḨ\nḩḩ\nḪḪ\nḫḫ\nḬḬ\nḭḭ\nḮḮ\nḯḯ\nḰḰ\nḱḱ\nḲḲ\nḳḳ\nḴḴ\nḵḵ\nḶḶ\nḷḷ\nḸḸ\nḹḹ\nḺḺ\nḻḻ\nḼḼ\nḽḽ\nḾḾ\nḿḿ\nṀṀ\nṁṁ\nṂṂ\nṃṃ\nṄṄ\nṅṅ\nṆṆ\nṇṇ\nṈṈ\nṉṉ\nṊṊ\nṋṋ\nṌṌ\nṍṍ\nṎṎ\nṏṏ\nṐṐ\nṑṑ\nṒṒ\nṓṓ\nṔṔ\nṕṕ\nṖṖ\nṗṗ\nṘṘ\nṙṙ\nṚṚ\nṛṛ\nṜṜ\nṝṝ\nṞṞ\nṟṟ\nṠṠ\nṡṡ\nṢṢ\nṣṣ\nṤṤ\nṥṥ\nṦṦ\nṧṧ\nṨṨ\nṩṩ\nṪṪ\nṫṫ\nṬṬ\nṭṭ\nṮṮ\nṯṯ\nṰṰ\nṱṱ\nṲṲ\nṳṳ\nṴṴ\nṵṵ\nṶṶ\nṷṷ\nṸṸ\nṹṹ\nṺṺ\nṻṻ\nṼṼ\nṽṽ\nṾṾ\nṿṿ\nẀẀ\nẁẁ\nẂẂ\nẃẃ\nẄẄ\nẅẅ\nẆẆ\nẇẇ\nẈẈ\nẉẉ\nẊẊ\nẋẋ\nẌẌ\nẍẍ\nẎẎ\nẏẏ\nẐẐ\nẑẑ\nẒẒ\nẓẓ\nẔẔ\nẕẕ\nẖẖ\nẗẗ\nẘẘ\nẙẙ\nẚaʾ\nẛṡ\nẠẠ\nạạ\nẢẢ\nảả\nẤẤ\nấấ\nẦẦ\nầầ\nẨẨ\nẩẩ\nẪẪ\nẫẫ\nẬẬ\nậậ\nẮẮ\nắắ\nẰẰ\nằằ\nẲẲ\nẳẳ\nẴẴ\nẵẵ\nẶẶ\nặặ\nẸẸ\nẹẹ\nẺẺ\nẻẻ\nẼẼ\nẽẽ\nẾẾ\nếế\nỀỀ\nềề\nỂỂ\nểể\nỄỄ\nễễ\nỆỆ\nệệ\nỈỈ\nỉỉ\nỊỊ\nịị\nỌỌ\nọọ\nỎỎ\nỏỏ\nỐỐ\nốố\nỒỒ\nồồ\nỔỔ\nổổ\nỖỖ\nỗỗ\nỘỘ\nộộ\nỚỚ\nớớ\nỜỜ\nờờ\nỞỞ\nởở\nỠỠ\nỡỡ\nỢỢ\nợợ\nỤỤ\nụụ\nỦỦ\nủủ\nỨỨ\nứứ\nỪỪ\nừừ\nỬỬ\nửử\nỮỮ\nữữ\nỰỰ\nựự\nỲỲ\nỳỳ\nỴỴ\nỵỵ\nỶỶ\nỷỷ\nỸỸ\nỹỹ\nἀἀ\nἁἁ\nἂἂ\nἃἃ\nἄἄ\nἅἅ\nἆἆ\nἇἇ\nἈἈ\nἉἉ\nἊἊ\nἋἋ\nἌἌ\nἍἍ\nἎἎ\nἏἏ\nἐἐ\nἑἑ\nἒἒ\nἓἓ\nἔἔ\nἕἕ\nἘἘ\nἙἙ\nἚἚ\nἛἛ\nἜἜ\nἝἝ\nἠἠ\nἡἡ\nἢἢ\nἣἣ\nἤἤ\nἥἥ\nἦἦ\nἧἧ\nἨἨ\nἩἩ\nἪἪ\nἫἫ\nἬἬ\nἭἭ\nἮἮ\nἯἯ\nἰἰ\nἱἱ\nἲἲ\nἳἳ\nἴἴ\nἵἵ\nἶἶ\nἷἷ\nἸἸ\nἹἹ\nἺἺ\nἻἻ\nἼἼ\nἽἽ\nἾἾ\nἿἿ\nὀὀ\nὁὁ\nὂὂ\nὃὃ\nὄὄ\nὅὅ\nὈὈ\nὉὉ\nὊὊ\nὋὋ\nὌὌ\nὍὍ\nὐὐ\nὑὑ\nὒὒ\nὓὓ\nὔὔ\nὕὕ\nὖὖ\nὗὗ\nὙὙ\nὛὛ\nὝὝ\nὟὟ\nὠὠ\nὡὡ\nὢὢ\nὣὣ\nὤὤ\nὥὥ\nὦὦ\nὧὧ\nὨὨ\nὩὩ\nὪὪ\nὫὫ\nὬὬ\nὭὭ\nὮὮ\nὯὯ\nὰὰ\nάά\nὲὲ\nέέ\nὴὴ\nήή\nὶὶ\nίί\nὸὸ\nόό\nὺὺ\nύύ\nὼὼ\nώώ\nᾀᾀ\nᾁᾁ\nᾂᾂ\nᾃᾃ\nᾄᾄ\nᾅᾅ\nᾆᾆ\nᾇᾇ\nᾈᾈ\nᾉᾉ\nᾊᾊ\nᾋᾋ\nᾌᾌ\nᾍᾍ\nᾎᾎ\nᾏᾏ\nᾐᾐ\nᾑᾑ\nᾒᾒ\nᾓᾓ\nᾔᾔ\nᾕᾕ\nᾖᾖ\nᾗᾗ\nᾘᾘ\nᾙᾙ\nᾚᾚ\nᾛᾛ\nᾜᾜ\nᾝᾝ\nᾞᾞ\nᾟᾟ\nᾠᾠ\nᾡᾡ\nᾢᾢ\nᾣᾣ\nᾤᾤ\nᾥᾥ\nᾦᾦ\nᾧᾧ\nᾨᾨ\nᾩᾩ\nᾪᾪ\nᾫᾫ\nᾬᾬ\nᾭᾭ\nᾮᾮ\nᾯᾯ\nᾰᾰ\nᾱᾱ\nᾲᾲ\nᾳᾳ\nᾴᾴ\nᾶᾶ\nᾷᾷ\nᾸᾸ\nᾹᾹ\nᾺᾺ\nΆΆ\nᾼᾼ\n᾽ ̓\nιι\n᾿ ̓\n῀ ͂\n῁ ̈͂\nῂῂ\nῃῃ\nῄῄ\nῆῆ\nῇῇ\nῈῈ\nΈΈ\nῊῊ\nΉΉ\nῌῌ\n῍ ̓̀\n῎ ̓́\n῏ ̓͂\nῐῐ\nῑῑ\nῒῒ\nΐΐ\nῖῖ\nῗῗ\nῘῘ\nῙῙ\nῚῚ\nΊΊ\n῝ ̔̀\n῞ ̔́\n῟ ̔͂\nῠῠ\nῡῡ\nῢῢ\nΰΰ\nῤῤ\nῥῥ\nῦῦ\nῧῧ\nῨῨ\nῩῩ\nῪῪ\nΎΎ\nῬῬ\n῭ ̈̀\n΅ ̈́\n``\nῲῲ\nῳῳ\nῴῴ\nῶῶ\nῷῷ\nῸῸ\nΌΌ\nῺῺ\nΏΏ\nῼῼ\n´ ́\n῾ ̔\n  \n  \n  \n  \n  \n  \n  \n  \n  \n  \n  \n‑‐\n‗ ̳\n․.\n‥..\n…...\n  \n″′′\n‴′′′\n‶‵‵\n‷‵‵‵\n‼!!\n‾ ̅\n⁇??\n⁈?!\n⁉!?\n⁗′′′′\n  \n⁰0\nⁱi\n⁴4\n⁵5\n⁶6\n⁷7\n⁸8\n⁹9\n⁺+\n⁻−\n⁼=\n⁽(\n⁾)\nⁿn\n₀0\n₁1\n₂2\n₃3\n₄4\n₅5\n₆6\n₇7\n₈8\n₉9\n₊+\n₋−\n₌=\n₍(\n₎)\nₐa\nₑe\nₒo\nₓx\nₔə\n₨Rs\n℀a/c\n℁a/s\nℂC\n℃°C\n℅c/o\n℆c/u\nℇƐ\n℉°F\nℊg\nℋH\nℌH\nℍH\nℎh\nℏħ\nℐI\nℑI\nℒL\nℓl\nℕN\n№No\nℙP\nℚQ\nℛR\nℜR\nℝR\n℠SM\n℡TEL\n™TM\nℤZ\nΩΩ\nℨZ\nKK\nÅÅ\nℬB\nℭC\nℯe\nℰE\nℱF\nℳM\nℴo\nℵא\nℶב\nℷג\nℸד\nℹi\n℻FAX\nℼπ\nℽγ\nℾΓ\nℿΠ\n⅀∑\nⅅD\nⅆd\nⅇe\nⅈi\nⅉj\n⅓1⁄3\n⅔2⁄3\n⅕1⁄5\n⅖2⁄5\n⅗3⁄5\n⅘4⁄5\n⅙1⁄6\n⅚5⁄6\n⅛1⁄8\n⅜3⁄8\n⅝5⁄8\n⅞7⁄8\n⅟1⁄\nⅠI\nⅡII\nⅢIII\nⅣIV\nⅤV\nⅥVI\nⅦVII\nⅧVIII\nⅨIX\nⅩX\nⅪXI\nⅫXII\nⅬL\nⅭC\nⅮD\nⅯM\nⅰi\nⅱii\nⅲiii\nⅳiv\nⅴv\nⅵvi\nⅶvii\nⅷviii\nⅸix\nⅹx\nⅺxi\nⅻxii\nⅼl\nⅽc\nⅾd\nⅿm\n↚↚\n↛↛\n↮↮\n⇍⇍\n⇎⇎\n⇏⇏\n∄∄\n∉∉\n∌∌\n∤∤\n∦∦\n∬∫∫\n∭∫∫∫\n∯∮∮\n∰∮∮∮\n≁≁\n≄≄\n≇≇\n≉≉\n≠≠\n≢≢\n≭≭\n≮≮\n≯≯\n≰≰\n≱≱\n≴≴\n≵≵\n≸≸\n≹≹\n⊀⊀\n⊁⊁\n⊄⊄\n⊅⊅\n⊈⊈\n⊉⊉\n⊬⊬\n⊭⊭\n⊮⊮\n⊯⊯\n⋠⋠\n⋡⋡\n⋢⋢\n⋣⋣\n⋪⋪\n⋫⋫\n⋬⋬\n⋭⋭\n〈〈\n〉〉\n①1\n②2\n③3\n④4\n⑤5\n⑥6\n⑦7\n⑧8\n⑨9\n⑩10\n⑪11\n⑫12\n⑬13\n⑭14\n⑮15\n⑯16\n⑰17\n⑱18\n⑲19\n⑳20\n⑴(1)\n⑵(2)\n⑶(3)\n⑷(4)\n⑸(5)\n⑹(6)\n⑺(7)\n⑻(8)\n⑼(9)\n⑽(10)\n⑾(11)\n⑿(12)\n⒀(13)\n⒁(14)\n⒂(15)\n⒃(16)\n⒄(17)\n⒅(18)\n⒆(19)\n⒇(20)\n⒈1.\n⒉2.\n⒊3.\n⒋4.\n⒌5.\n⒍6.\n⒎7.\n⒏8.\n⒐9.\n⒑10.\n⒒11.\n⒓12.\n⒔13.\n⒕14.\n⒖15.\n⒗16.\n⒘17.\n⒙18.\n⒚19.\n⒛20.\n⒜(a)\n⒝(b)\n⒞(c)\n⒟(d)\n⒠(e)\n⒡(f)\n⒢(g)\n⒣(h)\n⒤(i)\n⒥(j)\n⒦(k)\n⒧(l)\n⒨(m)\n⒩(n)\n⒪(o)\n⒫(p)\n⒬(q)\n⒭(r)\n⒮(s)\n⒯(t)\n⒰(u)\n⒱(v)\n⒲(w)\n⒳(x)\n⒴(y)\n⒵(z)\nⒶA\nⒷB\nⒸC\nⒹD\nⒺE\nⒻF\nⒼG\nⒽH\nⒾI\nⒿJ\nⓀK\nⓁL\nⓂM\nⓃN\nⓄO\nⓅP\nⓆQ\nⓇR\nⓈS\nⓉT\nⓊU\nⓋV\nⓌW\nⓍX\nⓎY\nⓏZ\nⓐa\nⓑb\nⓒc\nⓓd\nⓔe\nⓕf\nⓖg\nⓗh\nⓘi\nⓙj\nⓚk\nⓛl\nⓜm\nⓝn\nⓞo\nⓟp\nⓠq\nⓡr\nⓢs\nⓣt\nⓤu\nⓥv\nⓦw\nⓧx\nⓨy\nⓩz\n⓪0\n⨌∫∫∫∫\n⩴::=\n⩵==\n⩶===\n⫝̸⫝̸\nⱼj\nⱽV\nⵯⵡ\n⺟母\n⻳龟\n⼀一\n⼁丨\n⼂丶\n⼃丿\n⼄乙\n⼅亅\n⼆二\n⼇亠\n⼈人\n⼉儿\n⼊入\n⼋八\n⼌冂\n⼍冖\n⼎冫\n⼏几\n⼐凵\n⼑刀\n⼒力\n⼓勹\n⼔匕\n⼕匚\n⼖匸\n⼗十\n⼘卜\n⼙卩\n⼚厂\n⼛厶\n⼜又\n⼝口\n⼞囗\n⼟土\n⼠士\n⼡夂\n⼢夊\n⼣夕\n⼤大\n⼥女\n⼦子\n⼧宀\n⼨寸\n⼩小\n⼪尢\n⼫尸\n⼬屮\n⼭山\n⼮巛\n⼯工\n⼰己\n⼱巾\n⼲干\n⼳幺\n⼴广\n⼵廴\n⼶廾\n⼷弋\n⼸弓\n⼹彐\n⼺彡\n⼻彳\n⼼心\n⼽戈\n⼾戶\n⼿手\n⽀支\n⽁攴\n⽂文\n⽃斗\n⽄斤\n⽅方\n⽆无\n⽇日\n⽈曰\n⽉月\n⽊木\n⽋欠\n⽌止\n⽍歹\n⽎殳\n⽏毋\n⽐比\n⽑毛\n⽒氏\n⽓气\n⽔水\n⽕火\n⽖爪\n⽗父\n⽘爻\n⽙爿\n⽚片\n⽛牙\n⽜牛\n⽝犬\n⽞玄\n⽟玉\n⽠瓜\n⽡瓦\n⽢甘\n⽣生\n⽤用\n⽥田\n⽦疋\n⽧疒\n⽨癶\n⽩白\n⽪皮\n⽫皿\n⽬目\n⽭矛\n⽮矢\n⽯石\n⽰示\n⽱禸\n⽲禾\n⽳穴\n⽴立\n⽵竹\n⽶米\n⽷糸\n⽸缶\n⽹网\n⽺羊\n⽻羽\n⽼老\n⽽而\n⽾耒\n⽿耳\n⾀聿\n⾁肉\n⾂臣\n⾃自\n⾄至\n⾅臼\n⾆舌\n⾇舛\n⾈舟\n⾉艮\n⾊色\n⾋艸\n⾌虍\n⾍虫\n⾎血\n⾏行\n⾐衣\n⾑襾\n⾒見\n⾓角\n⾔言\n⾕谷\n⾖豆\n⾗豕\n⾘豸\n⾙貝\n⾚赤\n⾛走\n⾜足\n⾝身\n⾞車\n⾟辛\n⾠辰\n⾡辵\n⾢邑\n⾣酉\n⾤釆\n⾥里\n⾦金\n⾧長\n⾨門\n⾩阜\n⾪隶\n⾫隹\n⾬雨\n⾭靑\n⾮非\n⾯面\n⾰革\n⾱韋\n⾲韭\n⾳音\n⾴頁\n⾵風\n⾶飛\n⾷食\n⾸首\n⾹香\n⾺馬\n⾻骨\n⾼高\n⾽髟\n⾾鬥\n⾿鬯\n⿀鬲\n⿁鬼\n⿂魚\n⿃鳥\n⿄鹵\n⿅鹿\n⿆麥\n⿇麻\n⿈黃\n⿉黍\n⿊黑\n⿋黹\n⿌黽\n⿍鼎\n⿎鼓\n⿏鼠\n⿐鼻\n⿑齊\n⿒齒\n⿓龍\n⿔龜\n⿕龠\n　 \n〶〒\n〸十\n〹卄\n〺卅\nがが\nぎぎ\nぐぐ\nげげ\nごご\nざざ\nじじ\nずず\nぜぜ\nぞぞ\nだだ\nぢぢ\nづづ\nでで\nどど\nばば\nぱぱ\nびび\nぴぴ\nぶぶ\nぷぷ\nべべ\nぺぺ\nぼぼ\nぽぽ\nゔゔ\n゛ ゙\n゜ ゚\nゞゞ\nゟより\nガガ\nギギ\nググ\nゲゲ\nゴゴ\nザザ\nジジ\nズズ\nゼゼ\nゾゾ\nダダ\nヂヂ\nヅヅ\nデデ\nドド\nババ\nパパ\nビビ\nピピ\nブブ\nププ\nベベ\nペペ\nボボ\nポポ\nヴヴ\nヷヷ\nヸヸ\nヹヹ\nヺヺ\nヾヾ\nヿコト\nㄱᄀ\nㄲᄁ\nㄳᆪ\nㄴᄂ\nㄵᆬ\nㄶᆭ\nㄷᄃ\nㄸᄄ\nㄹᄅ\nㄺᆰ\nㄻᆱ\nㄼᆲ\nㄽᆳ\nㄾᆴ\nㄿᆵ\nㅀᄚ\nㅁᄆ\nㅂᄇ\nㅃᄈ\nㅄᄡ\nㅅᄉ\nㅆᄊ\nㅇᄋ\nㅈᄌ\nㅉᄍ\nㅊᄎ\nㅋᄏ\nㅌᄐ\nㅍᄑ\nㅎᄒ\nㅏᅡ\nㅐᅢ\nㅑᅣ\nㅒᅤ\nㅓᅥ\nㅔᅦ\nㅕᅧ\nㅖᅨ\nㅗᅩ\nㅘᅪ\nㅙᅫ\nㅚᅬ\nㅛᅭ\nㅜᅮ\nㅝᅯ\nㅞᅰ\nㅟᅱ\nㅠᅲ\nㅡᅳ\nㅢᅴ\nㅣᅵ\nㅤᅠ\nㅥᄔ\nㅦᄕ\nㅧᇇ\nㅨᇈ\nㅩᇌ\nㅪᇎ\nㅫᇓ\nㅬᇗ\nㅭᇙ\nㅮᄜ\nㅯᇝ\nㅰᇟ\nㅱᄝ\nㅲᄞ\nㅳᄠ\nㅴᄢ\nㅵᄣ\nㅶᄧ\nㅷᄩ\nㅸᄫ\nㅹᄬ\nㅺᄭ\nㅻᄮ\nㅼᄯ\nㅽᄲ\nㅾᄶ\nㅿᅀ\nㆀᅇ\nㆁᅌ\nㆂᇱ\nㆃᇲ\nㆄᅗ\nㆅᅘ\nㆆᅙ\nㆇᆄ\nㆈᆅ\nㆉᆈ\nㆊᆑ\nㆋᆒ\nㆌᆔ\nㆍᆞ\nㆎᆡ\n㆒一\n㆓二\n㆔三\n㆕四\n㆖上\n㆗中\n㆘下\n㆙甲\n㆚乙\n㆛丙\n㆜丁\n㆝天\n㆞地\n㆟人\n㈀(ᄀ)\n㈁(ᄂ)\n㈂(ᄃ)\n㈃(ᄅ)\n㈄(ᄆ)\n㈅(ᄇ)\n㈆(ᄉ)\n㈇(ᄋ)\n㈈(ᄌ)\n㈉(ᄎ)\n㈊(ᄏ)\n㈋(ᄐ)\n㈌(ᄑ)\n㈍(ᄒ)\n㈎(가)\n㈏(나)\n㈐(다)\n㈑(라)\n㈒(마)\n㈓(바)\n㈔(사)\n㈕(아)\n㈖(자)\n㈗(차)\n㈘(카)\n㈙(타)\n㈚(파)\n㈛(하)\n㈜(주)\n㈝(오전)\n㈞(오후)\n㈠(一)\n㈡(二)\n㈢(三)\n㈣(四)\n㈤(五)\n㈥(六)\n㈦(七)\n㈧(八)\n㈨(九)\n㈩(十)\n㈪(月)\n㈫(火)\n㈬(水)\n㈭(木)\n㈮(金)\n㈯(土)\n㈰(日)\n㈱(株)\n㈲(有)\n㈳(社)\n㈴(名)\n㈵(特)\n㈶(財)\n㈷(祝)\n㈸(労)\n㈹(代)\n㈺(呼)\n㈻(学)\n㈼(監)\n㈽(企)\n㈾(資)\n㈿(協)\n㉀(祭)\n㉁(休)\n㉂(自)\n㉃(至)\n㉐PTE\n㉑21\n㉒22\n㉓23\n㉔24\n㉕25\n㉖26\n㉗27\n㉘28\n㉙29\n㉚30\n㉛31\n㉜32\n㉝33\n㉞34\n㉟35\n㉠ᄀ\n㉡ᄂ\n㉢ᄃ\n㉣ᄅ\n㉤ᄆ\n㉥ᄇ\n㉦ᄉ\n㉧ᄋ\n㉨ᄌ\n㉩ᄎ\n㉪ᄏ\n㉫ᄐ\n㉬ᄑ\n㉭ᄒ\n㉮가\n㉯나\n㉰다\n㉱라\n㉲마\n㉳바\n㉴사\n㉵아\n㉶자\n㉷차\n㉸카\n㉹타\n㉺파\n㉻하\n㉼참고\n㉽주의\n㉾우\n㊀一\n㊁二\n㊂三\n㊃四\n㊄五\n㊅六\n㊆七\n㊇八\n㊈九\n㊉十\n㊊月\n㊋火\n㊌水\n㊍木\n㊎金\n㊏土\n㊐日\n㊑株\n㊒有\n㊓社\n㊔名\n㊕特\n㊖財\n㊗祝\n㊘労\n㊙秘\n㊚男\n㊛女\n㊜適\n㊝優\n㊞印\n㊟注\n㊠項\n㊡休\n㊢写\n㊣正\n㊤上\n㊥中\n㊦下\n㊧左\n㊨右\n㊩医\n㊪宗\n㊫学\n㊬監\n㊭企\n㊮資\n㊯協\n㊰夜\n㊱36\n㊲37\n㊳38\n㊴39\n㊵40\n㊶41\n㊷42\n㊸43\n㊹44\n㊺45\n㊻46\n㊼47\n㊽48\n㊾49\n㊿50\n㋀1月\n㋁2月\n㋂3月\n㋃4月\n㋄5月\n㋅6月\n㋆7月\n㋇8月\n㋈9月\n㋉10月\n㋊11月\n㋋12月\n㋌Hg\n㋍erg\n㋎eV\n㋏LTD\n㋐ア\n㋑イ\n㋒ウ\n㋓エ\n㋔オ\n㋕カ\n㋖キ\n㋗ク\n㋘ケ\n㋙コ\n㋚サ\n㋛シ\n㋜ス\n㋝セ\n㋞ソ\n㋟タ\n㋠チ\n㋡ツ\n㋢テ\n㋣ト\n㋤ナ\n㋥ニ\n㋦ヌ\n㋧ネ\n㋨ノ\n㋩ハ\n㋪ヒ\n㋫フ\n㋬ヘ\n㋭ホ\n㋮マ\n㋯ミ\n㋰ム\n㋱メ\n㋲モ\n㋳ヤ\n㋴ユ\n㋵ヨ\n㋶ラ\n㋷リ\n㋸ル\n㋹レ\n㋺ロ\n㋻ワ\n㋼ヰ\n㋽ヱ\n㋾ヲ\n㌀アパート\n㌁アルファ\n㌂アンペア\n㌃アール\n㌄イニング\n㌅インチ\n㌆ウォン\n㌇エスクード\n㌈エーカー\n㌉オンス\n㌊オーム\n㌋カイリ\n㌌カラット\n㌍カロリー\n㌎ガロン\n㌏ガンマ\n㌐ギガ\n㌑ギニー\n㌒キュリー\n㌓ギルダー\n㌔キロ\n㌕キログラム\n㌖キロメートル\n㌗キロワット\n㌘グラム\n㌙グラムトン\n㌚クルゼイロ\n㌛クローネ\n㌜ケース\n㌝コルナ\n㌞コーポ\n㌟サイクル\n㌠サンチーム\n㌡シリング\n㌢センチ\n㌣セント\n㌤ダース\n㌥デシ\n㌦ドル\n㌧トン\n㌨ナノ\n㌩ノット\n㌪ハイツ\n㌫パーセント\n㌬パーツ\n㌭バーレル\n㌮ピアストル\n㌯ピクル\n㌰ピコ\n㌱ビル\n㌲ファラッド\n㌳フィート\n㌴ブッシェル\n㌵フラン\n㌶ヘクタール\n㌷ペソ\n㌸ペニヒ\n㌹ヘルツ\n㌺ペンス\n㌻ページ\n㌼ベータ\n㌽ポイント\n㌾ボルト\n㌿ホン\n㍀ポンド\n㍁ホール\n㍂ホーン\n㍃マイクロ\n㍄マイル\n㍅マッハ\n㍆マルク\n㍇マンション\n㍈ミクロン\n㍉ミリ\n㍊ミリバール\n㍋メガ\n㍌メガトン\n㍍メートル\n㍎ヤード\n㍏ヤール\n㍐ユアン\n㍑リットル\n㍒リラ\n㍓ルピー\n㍔ルーブル\n㍕レム\n㍖レントゲン\n㍗ワット\n㍘0点\n㍙1点\n㍚2点\n㍛3点\n㍜4点\n㍝5点\n㍞6点\n㍟7点\n㍠8点\n㍡9点\n㍢10点\n㍣11点\n㍤12点\n㍥13点\n㍦14点\n㍧15点\n㍨16点\n㍩17点\n㍪18点\n㍫19点\n㍬20点\n㍭21点\n㍮22点\n㍯23点\n㍰24点\n㍱hPa\n㍲da\n㍳AU\n㍴bar\n㍵oV\n㍶pc\n㍷dm\n㍸dm2\n㍹dm3\n㍺IU\n㍻平成\n㍼昭和\n㍽大正\n㍾明治\n㍿株式会社\n㎀pA\n㎁nA\n㎂μA\n㎃mA\n㎄kA\n㎅KB\n㎆MB\n㎇GB\n㎈cal\n㎉kcal\n㎊pF\n㎋nF\n㎌μF\n㎍μg\n㎎mg\n㎏kg\n㎐Hz\n㎑kHz\n㎒MHz\n㎓GHz\n㎔THz\n㎕μl\n㎖ml\n㎗dl\n㎘kl\n㎙fm\n㎚nm\n㎛μm\n㎜mm\n㎝cm\n㎞km\n㎟mm2\n㎠cm2\n㎡m2\n㎢km2\n㎣mm3\n㎤cm3\n㎥m3\n㎦km3\n㎧m∕s\n㎨m∕s2\n㎩Pa\n㎪kPa\n㎫MPa\n㎬GPa\n㎭rad\n㎮rad∕s\n㎯rad∕s2\n㎰ps\n㎱ns\n㎲μs\n㎳ms\n㎴pV\n㎵nV\n㎶μV\n㎷mV\n㎸kV\n㎹MV\n㎺pW\n㎻nW\n㎼μW\n㎽mW\n㎾kW\n㎿MW\n㏀kΩ\n㏁MΩ\n㏂a.m.\n㏃Bq\n㏄cc\n㏅cd\n㏆C∕kg\n㏇Co.\n㏈dB\n㏉Gy\n㏊ha\n㏋HP\n㏌in\n㏍KK\n㏎KM\n㏏kt\n㏐lm\n㏑ln\n㏒log\n㏓lx\n㏔mb\n㏕mil\n㏖mol\n㏗PH\n㏘p.m.\n㏙PPM\n㏚PR\n㏛sr\n㏜Sv\n㏝Wb\n㏞V∕m\n㏟A∕m\n㏠1日\n㏡2日\n㏢3日\n㏣4日\n㏤5日\n㏥6日\n㏦7日\n㏧8日\n㏨9日\n㏩10日\n㏪11日\n㏫12日\n㏬13日\n㏭14日\n㏮15日\n㏯16日\n㏰17日\n㏱18日\n㏲19日\n㏳20日\n㏴21日\n㏵22日\n㏶23日\n㏷24日\n㏸25日\n㏹26日\n㏺27日\n㏻28日\n㏼29日\n㏽30日\n㏾31日\n㏿gal\n豈豈\n更更\n車車\n賈賈\n滑滑\n串串\n句句\n龜龜\n龜龜\n契契\n金金\n喇喇\n奈奈\n懶懶\n癩癩\n羅羅\n蘿蘿\n螺螺\n裸裸\n邏邏\n樂樂\n洛洛\n烙烙\n珞珞\n落落\n酪酪\n駱駱\n亂亂\n卵卵\n欄欄\n爛爛\n蘭蘭\n鸞鸞\n嵐嵐\n濫濫\n藍藍\n襤襤\n拉拉\n臘臘\n蠟蠟\n廊廊\n朗朗\n浪浪\n狼狼\n郎郎\n來來\n冷冷\n勞勞\n擄擄\n櫓櫓\n爐爐\n盧盧\n老老\n蘆蘆\n虜虜\n路路\n露露\n魯魯\n鷺鷺\n碌碌\n祿祿\n綠綠\n菉菉\n錄錄\n鹿鹿\n論論\n壟壟\n弄弄\n籠籠\n聾聾\n牢牢\n磊磊\n賂賂\n雷雷\n壘壘\n屢屢\n樓樓\n淚淚\n漏漏\n累累\n縷縷\n陋陋\n勒勒\n肋肋\n凜凜\n凌凌\n稜稜\n綾綾\n菱菱\n陵陵\n讀讀\n拏拏\n樂樂\n諾諾\n丹丹\n寧寧\n怒怒\n率率\n異異\n北北\n磻磻\n便便\n復復\n不不\n泌泌\n數數\n索索\n參參\n塞塞\n省省\n葉葉\n說說\n殺殺\n辰辰\n沈沈\n拾拾\n若若\n掠掠\n略略\n亮亮\n兩兩\n凉凉\n梁梁\n糧糧\n良良\n諒諒\n量量\n勵勵\n呂呂\n女女\n廬廬\n旅旅\n濾濾\n礪礪\n閭閭\n驪驪\n麗麗\n黎黎\n力力\n曆曆\n歷歷\n轢轢\n年年\n憐憐\n戀戀\n撚撚\n漣漣\n煉煉\n璉璉\n秊秊\n練練\n聯聯\n輦輦\n蓮蓮\n連連\n鍊鍊\n列列\n劣劣\n咽咽\n烈烈\n裂裂\n說說\n廉廉\n念念\n捻捻\n殮殮\n簾簾\n獵獵\n令令\n囹囹\n寧寧\n嶺嶺\n怜怜\n玲玲\n瑩瑩\n羚羚\n聆聆\n鈴鈴\n零零\n靈靈\n領領\n例例\n禮禮\n醴醴\n隸隸\n惡惡\n了了\n僚僚\n寮寮\n尿尿\n料料\n樂樂\n燎燎\n療療\n蓼蓼\n遼遼\n龍龍\n暈暈\n阮阮\n劉劉\n杻杻\n柳柳\n流流\n溜溜\n琉琉\n留留\n硫硫\n紐紐\n類類\n六六\n戮戮\n陸陸\n倫倫\n崙崙\n淪淪\n輪輪\n律律\n慄慄\n栗栗\n率率\n隆隆\n利利\n吏吏\n履履\n易易\n李李\n梨梨\n泥泥\n理理\n痢痢\n罹罹\n裏裏\n裡裡\n里里\n離離\n匿匿\n溺溺\n吝吝\n燐燐\n璘璘\n藺藺\n隣隣\n鱗鱗\n麟麟\n林林\n淋淋\n臨臨\n立立\n笠笠\n粒粒\n狀狀\n炙炙\n識識\n什什\n茶茶\n刺刺\n切切\n度度\n拓拓\n糖糖\n宅宅\n洞洞\n暴暴\n輻輻\n行行\n降降\n見見\n廓廓\n兀兀\n嗀嗀\n塚塚\n晴晴\n凞凞\n猪猪\n益益\n礼礼\n神神\n祥祥\n福福\n靖靖\n精精\n羽羽\n蘒蘒\n諸諸\n逸逸\n都都\n飯飯\n飼飼\n館館\n鶴鶴\n侮侮\n僧僧\n免免\n勉勉\n勤勤\n卑卑\n喝喝\n嘆嘆\n器器\n塀塀\n墨墨\n層層\n屮屮\n悔悔\n慨慨\n憎憎\n懲懲\n敏敏\n既既\n暑暑\n梅梅\n海海\n渚渚\n漢漢\n煮煮\n爫爫\n琢琢\n碑碑\n社社\n祉祉\n祈祈\n祐祐\n祖祖\n祝祝\n禍禍\n禎禎\n穀穀\n突突\n節節\n練練\n縉縉\n繁繁\n署署\n者者\n臭臭\n艹艹\n艹艹\n著著\n褐褐\n視視\n謁謁\n謹謹\n賓賓\n贈贈\n辶辶\n逸逸\n難難\n響響\n頻頻\n並並\n况况\n全全\n侀侀\n充充\n冀冀\n勇勇\n勺勺\n喝喝\n啕啕\n喙喙\n嗢嗢\n塚塚\n墳墳\n奄奄\n奔奔\n婢婢\n嬨嬨\n廒廒\n廙廙\n彩彩\n徭徭\n惘惘\n慎慎\n愈愈\n憎憎\n慠慠\n懲懲\n戴戴\n揄揄\n搜搜\n摒摒\n敖敖\n晴晴\n朗朗\n望望\n杖杖\n歹歹\n殺殺\n流流\n滛滛\n滋滋\n漢漢\n瀞瀞\n煮煮\n瞧瞧\n爵爵\n犯犯\n猪猪\n瑱瑱\n甆甆\n画画\n瘝瘝\n瘟瘟\n益益\n盛盛\n直直\n睊睊\n着着\n磌磌\n窱窱\n節節\n类类\n絛絛\n練練\n缾缾\n者者\n荒荒\n華華\n蝹蝹\n襁襁\n覆覆\n視視\n調調\n諸諸\n請請\n謁謁\n諾諾\n諭諭\n謹謹\n變變\n贈贈\n輸輸\n遲遲\n醙醙\n鉶鉶\n陼陼\n難難\n靖靖\n韛韛\n響響\n頋頋\n頻頻\n鬒鬒\n龜龜\n𢡊𢡊\n𢡄𢡄\n𣏕𣏕\n㮝㮝\n䀘䀘\n䀹䀹\n𥉉𥉉\n𥳐𥳐\n𧻓𧻓\n齃齃\n龎龎\n！!\n＂\"\n＃#\n＄$\n％%\n＆&\n＇'\n（(\n）)\n＊*\n＋+\n，,\n－-\n．.\n／/\n０0\n１1\n２2\n３3\n４4\n５5\n６6\n７7\n８8\n９9\n：:\n；;\n＜<\n＝=\n＞>\n？?\n＠@\nＡA\nＢB\nＣC\nＤD\nＥE\nＦF\nＧG\nＨH\nＩI\nＪJ\nＫK\nＬL\nＭM\nＮN\nＯO\nＰP\nＱQ\nＲR\nＳS\nＴT\nＵU\nＶV\nＷW\nＸX\nＹY\nＺZ\n［[\n＼\\n］]\n＾^\n＿_\n｀`\nａa\nｂb\nｃc\nｄd\nｅe\nｆf\nｇg\nｈh\nｉi\nｊj\nｋk\nｌl\nｍm\nｎn\nｏo\nｐp\nｑq\nｒr\nｓs\nｔt\nｕu\nｖv\nｗw\nｘx\nｙy\nｚz\n｛{\n｜|\n｝}\n～~\n｟⦅\n｠⦆\n｡。\n｢「\n｣」\n､、\n･・\nｦヲ\nｧァ\nｨィ\nｩゥ\nｪェ\nｫォ\nｬャ\nｭュ\nｮョ\nｯッ\nｰー\nｱア\nｲイ\nｳウ\nｴエ\nｵオ\nｶカ\nｷキ\nｸク\nｹケ\nｺコ\nｻサ\nｼシ\nｽス\nｾセ\nｿソ\nﾀタ\nﾁチ\nﾂツ\nﾃテ\nﾄト\nﾅナ\nﾆニ\nﾇヌ\nﾈネ\nﾉノ\nﾊハ\nﾋヒ\nﾌフ\nﾍヘ\nﾎホ\nﾏマ\nﾐミ\nﾑム\nﾒメ\nﾓモ\nﾔヤ\nﾕユ\nﾖヨ\nﾗラ\nﾘリ\nﾙル\nﾚレ\nﾛロ\nﾜワ\nﾝン\nﾞ゙\nﾟ゚\nﾠᅠ\nﾡᄀ\nﾢᄁ\nﾣᆪ\nﾤᄂ\nﾥᆬ\nﾦᆭ\nﾧᄃ\nﾨᄄ\nﾩᄅ\nﾪᆰ\nﾫᆱ\nﾬᆲ\nﾭᆳ\nﾮᆴ\nﾯᆵ\nﾰᄚ\nﾱᄆ\nﾲᄇ\nﾳᄈ\nﾴᄡ\nﾵᄉ\nﾶᄊ\nﾷᄋ\nﾸᄌ\nﾹᄍ\nﾺᄎ\nﾻᄏ\nﾼᄐ\nﾽᄑ\nﾾᄒ\nￂᅡ\nￃᅢ\nￄᅣ\nￅᅤ\nￆᅥ\nￇᅦ\nￊᅧ\nￋᅨ\nￌᅩ\nￍᅪ\nￎᅫ\nￏᅬ\nￒᅭ\nￓᅮ\nￔᅯ\nￕᅰ\nￖᅱ\nￗᅲ\nￚᅳ\nￛᅴ\nￜᅵ\n￠¢\n￡£\n￢¬\n￣ ̄\n￤¦\n￥¥\n￦₩\n￨│\n￩←\n￪↑\n￫→\n￬↓\n￭■\n￮○\n";

		private static readonly int[][] SupportedChars =
		{
			new[] {0, 1000},
			new[] {12352, 12447},
			new[] {12448, 12543},
			new[] {19968, 40959},
			new[] {13312, 19967},
			new[] {131072, 173791},
			new[] {63744, 64255},
			new[] {194560, 195103},
			new[] {13056, 13311},
			new[] {12288, 12351},
			new[] {65280, 65535},
			new[] {8192, 8303},
			new[] {8352, 8399}
		};

		public static string NormalizeKD(string str)
		{
			var builder = new StringBuilder(str.Length);
			foreach (var c in str)
			{
				if (!Supported(c))
				{
					throw new PlatformNotSupportedException("the input string can't be normalized on this platform");
				}

				Substitute(c, builder);
			}

			return builder.ToString();
		}

		private static void Substitute(char c, StringBuilder builder)
		{
			for (var i = 0; i < SubstitutionTable.Length; i++)
			{
				var substituedChar = SubstitutionTable[i];
				if (substituedChar == c)
				{
					Substitute(i, builder);
					return;
				}

				if (substituedChar > c)
				{
					break;
				}

				while (SubstitutionTable[i] != '\n')
				{
					i++;
				}
			}

			builder.Append(c);
		}

		private static void Substitute(int pos, StringBuilder builder)
		{
			for (var i = pos + 1; i < SubstitutionTable.Length; i++)
			{
				if (SubstitutionTable[i] == '\n')
				{
					break;
				}

				builder.Append(SubstitutionTable[i]);
			}
		}

		private static bool Supported(char c)
		{
			return SupportedChars.Any(r => r[0] <= c && c <= r[1]);
		}
	}
}