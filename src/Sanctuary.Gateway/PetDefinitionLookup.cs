using System;
using System.Collections.Generic;
using System.Linq;

using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway.Admin;

public sealed record PetDefinitionMetadata(
    int ItemDefinitionId,
    int ModelId,
    string Name,
    int SpeciesId,
    int NameId,
    int ImageSetId,
    int TintId,
    string TintAlias)
{
    public PetDefinitionMetadata WithClientItem(ClientItemDefinition itemDefinition)
    {
        return this with
        {
            ItemDefinitionId = itemDefinition.Id,
            Name = ResolveClientPetName(itemDefinition, Name),
            NameId = itemDefinition.NameId > 0 ? itemDefinition.NameId : NameId,
            ImageSetId = itemDefinition.Icon.Id > 0 ? itemDefinition.Icon.Id : ImageSetId,
            TintId = itemDefinition.Icon.TintId > 0 ? itemDefinition.Icon.TintId : TintId,
            TintAlias = !string.IsNullOrWhiteSpace(itemDefinition.TintAlias)
                ? itemDefinition.TintAlias
                : TintAlias
        };
    }

    private static string ResolveClientPetName(ClientItemDefinition itemDefinition, string fallbackName)
    {
        var comment = itemDefinition.Comment?.Trim();
        if (string.IsNullOrWhiteSpace(comment) ||
            comment.StartsWith("Pet:", StringComparison.OrdinalIgnoreCase))
        {
            return fallbackName;
        }

        return comment[..Math.Min(comment.Length, 32)];
    }
}

public static class PetDefinitionLookup
{
    private const int DogSpeciesId = 652;
    private const int OtherSpeciesId = 653;

    private static readonly IReadOnlyDictionary<int, PetDefinitionMetadata> ItemDefinitions = new Dictionary<int, PetDefinitionMetadata>
    {
        [10399] = Pet(10399, 3173, "Domo", OtherSpeciesId, 6249, 6817, 0, ""),
        [2267] = Pet(2267, 2001, "Baby Penguin", OtherSpeciesId, 4724, 5939, 0, "dyetint"),
        [2272] = Pet(2272, 535, "Chicken", OtherSpeciesId, 4743, 6082, 0, "dyetint"),
        [2644] = Pet(2644, 515, "Black Lab", DogSpeciesId, 11435, 3198, 191, "lab"),
        [2684] = Pet(2684, 941, "Pug", DogSpeciesId, 11475, 3216, 210, "pug"),
        [2701] = Pet(2701, 50, "Toyger", OtherSpeciesId, 11492, 3195, 0, "toyger"),
        [4324] = Pet(4324, 1924, "Fritz", OtherSpeciesId, 4698, 5944, 343, "furry-red"),
        [7882] = Pet(7882, 28, "Bat", OtherSpeciesId, 2241, 6080, 0, "base1_default"),
        [7888] = Pet(7888, 403, "Rabbit", OtherSpeciesId, 4231, 6086, 512, "rabbit-brown"),
        [7890] = Pet(7890, 180, "Snake", OtherSpeciesId, 4259, 6088, 0, ""),
        [9049] = Pet(9049, 2008, "Treeble", OtherSpeciesId, 5427, 6479, 542, "treebleorange"),
        [10322] = Pet(10322, 3027, "Soccer Ball", OtherSpeciesId, 5630, 6622, 0, ""),
        [10323] = Pet(10323, 3027, "Soccer Ball", OtherSpeciesId, 5630, 6623, 0, ""),
        [10366] = Pet(10366, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 452, "groudhog-black"),
        [10367] = Pet(10367, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 455, "groudhog-blue"),
        [10368] = Pet(10368, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 453, "groudhog-orange"),
        [10369] = Pet(10369, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 454, "groudhog-pink"),
        [10370] = Pet(10370, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 451, "groudhog-red"),
        [10371] = Pet(10371, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 450, "groudhog-white"),
        [10392] = Pet(10392, 1454, "Husky", DogSpeciesId, 6373, 4457, 213, "darkmark"),
        [10407] = Pet(10407, 454, "Fairy Boy", OtherSpeciesId, 6415, 6860, 0, ""),
        [10408] = Pet(10408, 458, "Fairy Girl", OtherSpeciesId, 6416, 6857, 0, ""),
        [10469] = Pet(10469, 1689, "Jack-O-Lantern", OtherSpeciesId, 16635, 6939, 0, ""),
        [10489] = Pet(10489, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 92, "bubblegum"),
        [10490] = Pet(10490, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 97, "amethyst"),
        [10491] = Pet(10491, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 98, "grape"),
        [10492] = Pet(10492, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 100, "pumpkin"),
        [10493] = Pet(10493, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 101, "chocolate"),
        [10494] = Pet(10494, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 102, "raincloud"),
        [10495] = Pet(10495, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 112, "honey"),
        [10496] = Pet(10496, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 113, "tumbleweed"),
        [10497] = Pet(10497, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 117, "snow"),
        [10498] = Pet(10498, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 121, "charcoal"),
        [10987] = Pet(10987, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 450, "groudhog-white"),
        [10988] = Pet(10988, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 451, "groudhog-red"),
        [10989] = Pet(10989, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 452, "groudhog-black"),
        [10990] = Pet(10990, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 453, "groudhog-orange"),
        [10991] = Pet(10991, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 454, "groudhog-pink"),
        [10992] = Pet(10992, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 455, "groudhog-blue"),
        [10993] = Pet(10993, 2001, "Snowheart", OtherSpeciesId, 134699, 6253, 0, "penguin_valentines"),
        [10994] = Pet(10994, 2131, "She-Rex", OtherSpeciesId, 134698, 6255, 0, "trex-sherex"),
        [10996] = Pet(10996, 1771, "Lovasus", OtherSpeciesId, 134700, 6260, 0, "valentines"),
        [10998] = Pet(10998, 514, "Dalmatian", DogSpeciesId, 389081, 3206, 172, "dalmation"),
        [11614] = Pet(11614, 6, "Turtle", OtherSpeciesId, 170, 6091, 0, "dyetint"),
        [11939] = Pet(11939, 3885, "Clockwork Beetle", OtherSpeciesId, 389, 7675, 429, ""),
        [15493] = Pet(15493, 3284, "Wolf", OtherSpeciesId, 6270, 6873, 539, "base_grey"),
        [17557] = Pet(17557, 138, "Snow Hog", OtherSpeciesId, 118985, 6983, 0, ""),
        [23902] = Pet(23902, 172, "Summer", DogSpeciesId, 100673, 3209, 194, "solid"),
        [23906] = Pet(23906, 172, "Barkley", DogSpeciesId, 103974, 3207, 197, "darkmark"),
        [23912] = Pet(23912, 172, "Taz", DogSpeciesId, 100663, 3208, 202, "lightmark"),
        [23913] = Pet(23913, 172, "Pavel", DogSpeciesId, 103988, 3208, 203, "lightmark"),
        [23924] = Pet(23924, 364, "Allie", DogSpeciesId, 100664, 3210, 197, "darkmark"),
        [23929] = Pet(23929, 364, "Dashes", DogSpeciesId, 100654, 3211, 201, "lightmark"),
        [23931] = Pet(23931, 364, "Rascal", DogSpeciesId, 100674, 3211, 203, "lightmark"),
        [23943] = Pet(23943, 173, "Scooter", DogSpeciesId, 103986, 3213, 178, "darkmark"),
        [23957] = Pet(23957, 169, "Echo", DogSpeciesId, 7639, 3198, 185, "solid"),
        [23961] = Pet(23961, 169, "Max", DogSpeciesId, 100652, 3197, 178, "darkmark"),
        [23966] = Pet(23966, 169, "Arun", DogSpeciesId, 103978, 3199, 189, "spots"),
        [23978] = Pet(23978, 170, "Bruno", DogSpeciesId, 103976, 3200, 188, "darkmark"),
        [23982] = Pet(23982, 170, "Nox", DogSpeciesId, 100672, 3202, 190, "spots"),
        [23990] = Pet(23990, 171, "Diego", DogSpeciesId, 100668, 3203, 187, "darkmark"),
        [23995] = Pet(23995, 171, "Lake", DogSpeciesId, 103982, 3205, 180, "spots"),
        [24001] = Pet(24001, 169, "Roxie", DogSpeciesId, 18865, 3199, 183, "spots"),
        [24009] = Pet(24009, 50, "Mr. Kitty", OtherSpeciesId, 100665, 3191, 128, "plain"),
        [24021] = Pet(24021, 50, "Mojo", OtherSpeciesId, 100670, 3196, 140, "tuxedo"),
        [24022] = Pet(24022, 50, "Mittens", OtherSpeciesId, 103977, 3196, 141, "tuxedo"),
        [24027] = Pet(24027, 50, "Pooky", OtherSpeciesId, 103984, 3190, 146, "calico"),
        [24032] = Pet(24032, 50, "Zinx", OtherSpeciesId, 100879, 3194, 151, "tabby"),
        [24041] = Pet(24041, 50, "Gizmo", OtherSpeciesId, 100660, 3192, 160, "siamese"),
        [24044] = Pet(24044, 540, "Bubba", OtherSpeciesId, 100656, 3185, 127, "plain"),
        [24056] = Pet(24056, 540, "Luna", OtherSpeciesId, 100651, 3189, 139, "tuxedo"),
        [24062] = Pet(24062, 540, "Minnie", OtherSpeciesId, 100661, 3184, 145, "calico"),
        [24079] = Pet(24079, 540, "Tao", OtherSpeciesId, 100671, 3186, 162, "siamese"),
        [17840] = Pet(17840, 2001, "Baby Penguin", OtherSpeciesId, 4724, 5939, 262, "snow"),
        [17841] = Pet(17841, 2151, "Balloon Animal", DogSpeciesId, 4899, 6413, 227, "balloonred"),
        [17842] = Pet(17842, 3305, "Bat", OtherSpeciesId, 2241, 6937, 256, "dyetint-darkmatter"),
        [17843] = Pet(17843, 1773, "Icco", OtherSpeciesId, 419983, 5612, 234, "blacktip-brown"),
        [17844] = Pet(17844, 1921, "Crackers", OtherSpeciesId, 4660, 5941, 249, "duckbill-amethyst"),
        [17845] = Pet(17845, 1924, "Fritz", OtherSpeciesId, 4698, 5944, 227, "furry-red"),
        [17846] = Pet(17846, 1756, "Char", OtherSpeciesId, 24435, 5606, 243, "goldtips-black"),
        [17847] = Pet(17847, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 234, "groudhog-orange"),
        [17848] = Pet(17848, 1967, "Dumpling", OtherSpeciesId, 4842, 6063, 245, "hippo-aqua"),
        [17849] = Pet(17849, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 453, "groudhog-orange"),
        [17850] = Pet(17850, 3169, "Monkey", OtherSpeciesId, 7207, 6794, 262, "navy"),
        [17851] = Pet(17851, 2052, "Hedgewig", OtherSpeciesId, 4860, 6064, 260, "basic_white"),
        [17852] = Pet(17852, 1772, "Vivi", OtherSpeciesId, 419977, 5610, 260, "palomino-grey"),
        [17853] = Pet(17853, 1771, "Skip", OtherSpeciesId, 24666, 5608, 249, "basic-white"),
        [17854] = Pet(17854, 3286, "Pig", OtherSpeciesId, 5255, 6890, 258, "bubblegum"),
        [17855] = Pet(17855, 2057, "Alfy", OtherSpeciesId, 4839, 6076, 243, "bumpy-grey"),
        [17856] = Pet(17856, 1772, "Jaina", OtherSpeciesId, 24429, 5609, 249, "basic-tan"),
        [17857] = Pet(17857, 1769, "Screech", OtherSpeciesId, 4714, 5942, 256, "dark-purple"),
        [17858] = Pet(17858, 1924, "Munchy", OtherSpeciesId, 4695, 5943, 231, "blue-orange"),
        [17859] = Pet(17859, 1778, "Spike", OtherSpeciesId, 5106, 5607, 262, "orangepods-navy"),
        [17860] = Pet(17860, 1759, "Tex", OtherSpeciesId, 426595, 5858, 232, "spiked-brown"),
        [17861] = Pet(17861, 2052, "Archimedes", OtherSpeciesId, 4865, 6065, 231, "spots_copper"),
        [17862] = Pet(17862, 1773, "Rocco", OtherSpeciesId, 71253, 5613, 233, "spotty-navy"),
        [17863] = Pet(17863, 1774, "Cinnamon", OtherSpeciesId, 74594, 5619, 258, "spots-purple"),
        [17864] = Pet(17864, 2029, "Raja", OtherSpeciesId, 4857, 6066, 231, "tiger-orange"),
        [17865] = Pet(17865, 2008, "Treeble", OtherSpeciesId, 5427, 6479, 231, "treebleorange"),
        [17866] = Pet(17866, 1759, "Zax", OtherSpeciesId, 420571, 5857, 243, "trex-army"),
        [17867] = Pet(17867, 1760, "Iggy", OtherSpeciesId, 4560, 5617, 249, "triceratops-green"),
        [17868] = Pet(17868, 1774, "Chibi", OtherSpeciesId, 419955, 5618, 324, "basic-white"),
        [17869] = Pet(17869, 3284, "Wolf", OtherSpeciesId, 6270, 6873, 232, "base_metal"),
        [23930] = Pet(23930, 364, "York", DogSpeciesId, 103980, 3211, 202, "lightmark"),
        [23941] = Pet(23941, 173, "Pointy Puppy", DogSpeciesId, 100659, 3213, 182, "darkmark"),
        [23964] = Pet(23964, 169, "Floppy Puppy", DogSpeciesId, 103989, 3199, 179, "spots"),
        [55839] = Pet(55839, 514, "Dot", DogSpeciesId, 389081, 3206, 172, "dalmation"),
        [55840] = Pet(55840, 1018, "Ghost Dog", DogSpeciesId, 18855, 3804, 213, "darkmark"),
        [69824] = Pet(69824, 941, "Muggy", DogSpeciesId, 416495, 4639, 0, "dyetint"),
        [70352] = Pet(70352, 1756, "Dragon", OtherSpeciesId, 24435, 5606, 282, "goldtips-red"),
        [70429] = Pet(2272, 535, "Chicken", OtherSpeciesId, 4743, 6082, 0, "dyetint"),
        [70430] = Pet(11614, 6, "Turtle", OtherSpeciesId, 170, 6091, 0, "dyetint"),
        [70447] = Pet(70447, 1759, "T-Rex", OtherSpeciesId, 420571, 5857, 387, "trex-barndoor"),
        [76529] = Pet(76529, 1934, "Yule", OtherSpeciesId, 5057, 5614, 336, "basic-brown"),
        [76534] = Pet(76534, 1934, "Yule", OtherSpeciesId, 5057, 5614, 341, "basic-white"),
        [76535] = Pet(76535, 1968, "Garnet", OtherSpeciesId, 5059, 5615, 0, "dyetint"),
        [76906] = Pet(76906, 3479, "Lava Tiger", OtherSpeciesId, 6369, 7058, 0, "dyetint"),
        [77460] = Pet(77460, 152, "Cakeling", OtherSpeciesId, 6215, 6411, 0, ""),
        [78070] = Pet(78070, 3432, "Fire Pegasus", OtherSpeciesId, 435559, 7080, 290, "dyetint"),
        [10481] = Pet(10481, 3358, "Phoenix", OtherSpeciesId, 6574, 6955, 0, "0")
    };

    private static readonly IReadOnlyDictionary<string, PetDefinitionMetadata> ModelNames = new Dictionary<string, PetDefinitionMetadata>(StringComparer.OrdinalIgnoreCase)
    {
        ["babypenguin_m.adr"] = Pet(17840, 2001, "Baby Penguin", OtherSpeciesId, 4724, 5939, 262, "snow"),
        ["bat_01.adr"] = Pet(7882, 28, "Bat", OtherSpeciesId, 2241, 6080, 0, "base1_default"),
        ["beetle_clockwork.adr"] = Pet(11939, 3885, "Clockwork Beetle", OtherSpeciesId, 389, 7675, 429, ""),
        ["beetle_pincher.adr"] = Pet(17855, 2057, "Alfy", OtherSpeciesId, 4839, 6076, 429, "bumpy-grey"),
        ["cakeling_m_basic.adr"] = Pet(77460, 152, "Cakeling", OtherSpeciesId, 6215, 6411, 0, ""),
        ["cat_longhair.adr"] = Pet(2701, 540, "Longhair Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_longhairghost.adr"] = Pet(2701, 1013, "Ghost Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_longhair_calico.adr"] = Pet(2701, 3928, "Calico Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_longhair_tuxedo.adr"] = Pet(2701, 3929, "Tuxedo Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_shorthair.adr"] = Pet(2701, 50, "Shorthair Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_shorthairghost.adr"] = Pet(2701, 1014, "Ghost Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_shorthair_plain.adr"] = Pet(2701, 3930, "Shorthair Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_shorthair_striped.adr"] = Pet(2701, 3931, "Striped Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["cat_toygerghost.adr"] = Pet(2701, 1015, "Ghost Cat", OtherSpeciesId, 11492, 3195, 0, "dyetint"),
        ["chicken.adr"] = Pet(2272, 535, "Chicken", OtherSpeciesId, 4743, 6082, 0, "dyetint"),
        ["dog_large_balloondog.adr"] = Pet(17841, 2151, "Balloon Animal", DogSpeciesId, 4899, 6413, 227, "balloonred"),
        ["dog_large_dalmation.adr"] = Pet(10998, 514, "Dalmatian", DogSpeciesId, 389081, 3206, 172, "dalmation"),
        ["dog_large_floppy.adr"] = Pet(23964, 169, "Floppy Puppy", DogSpeciesId, 103989, 3199, 179, "spots"),
        ["dog_large_husky.adr"] = Pet(10392, 1454, "Husky", DogSpeciesId, 6373, 4457, 213, "darkmark"),
        ["dog_large_lab.adr"] = Pet(2644, 515, "Black Lab", DogSpeciesId, 11435, 3198, 191, "lab"),
        ["dog_large_pointy.adr"] = Pet(23964, 170, "Pointy Dog", DogSpeciesId, 103989, 3199, 179, "dyetint"),
        ["dog_large_pointyghost.adr"] = Pet(55840, 1018, "Ghost Dog", DogSpeciesId, 18855, 3804, 213, "darkmark"),
        ["dog_large_shortears.adr"] = Pet(23964, 171, "Short-Eared Dog", DogSpeciesId, 103989, 3199, 179, "dyetint"),
        ["dog_small_floppy.adr"] = Pet(23930, 172, "Floppy Puppy", DogSpeciesId, 103980, 3211, 202, "dyetint"),
        ["dog_small_fluffy.adr"] = Pet(23930, 364, "Fluffy Puppy", DogSpeciesId, 103980, 3211, 202, "lightmark"),
        ["dog_small_fluffyghost.adr"] = Pet(55840, 1022, "Ghost Puppy", DogSpeciesId, 18855, 3804, 213, "darkmark"),
        ["dog_small_pointy.adr"] = Pet(23941, 173, "Pointy Puppy", DogSpeciesId, 100659, 3213, 182, "darkmark"),
        ["dog_small_pug.adr"] = Pet(2684, 941, "Pug", DogSpeciesId, 11475, 3216, 210, "pug"),
        ["dog_small_pugmummy.adr"] = Pet(69824, 941, "Muggy", DogSpeciesId, 416495, 4639, 0, "dyetint"),
        ["dragon_m_basic.adr"] = Pet(70352, 1756, "Dragon", OtherSpeciesId, 24435, 5606, 282, "goldtips-red"),
        ["dragon_m_phoenix.adr"] = Pet(10481, 3358, "Phoenix", OtherSpeciesId, 6574, 6955, 0, "0"),
        ["dragon_m_spikes.adr"] = Pet(17859, 1778, "Spike", OtherSpeciesId, 5106, 5607, 286, "orangepods-navy"),
        ["duckbill_m.adr"] = Pet(17844, 1921, "Crackers", OtherSpeciesId, 4660, 5941, 373, "duckbill-amethyst"),
        ["groundhog_basic.adr"] = Pet(10992, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 455, "groudhog-blue"),
        ["groundhog_m.adr"] = Pet(10992, 2036, "Chuck", OtherSpeciesId, 4868, 6254, 455, "groudhog-blue"),
        ["groundhog_m_sharkfin.adr"] = Pet(10366, 3030, "Landshark", OtherSpeciesId, 5072, 6620, 452, "groudhog-black"),
        ["halloweenbat_01.adr"] = Pet(17842, 3305, "Bat", OtherSpeciesId, 2241, 6937, 256, "dyetint-darkmatter"),
        ["hippo_m_basic.adr"] = Pet(17848, 1967, "Dumpling", OtherSpeciesId, 4842, 6063, 245, "hippo-aqua"),
        ["jackolantern_m_01.adr"] = Pet(10469, 1689, "Jack-O-Lantern", OtherSpeciesId, 16635, 6939, 0, ""),
        ["marshmallow_critter.adr"] = Pet(10497, 2194, "Marshmallow", OtherSpeciesId, 5450, 6434, 117, "snow"),
        ["monkey_m.adr"] = Pet(17850, 3169, "Monkey", OtherSpeciesId, 7207, 6794, 262, "navy"),
        ["owl_m.adr"] = Pet(17851, 2052, "Owl", OtherSpeciesId, 4860, 6064, 260, "basic_white"),
        ["pegasus_m.adr"] = Pet(10996, 1771, "Lovasus", OtherSpeciesId, 134700, 6260, 0, "valentines"),
        ["penguin_basic.adr"] = Pet(10481, 2010, "Penguin", OtherSpeciesId, 6574, 6955, 0, "0"),
        ["pig_m_basic.adr"] = Pet(17854, 3286, "Pig", OtherSpeciesId, 5255, 6890, 92, "bubblegum"),
        ["pony_basic.adr"] = Pet(15493, 1773, "Pony", OtherSpeciesId, 6270, 6873, 0, "base_grey"),
        ["pony_f.adr"] = Pet(17852, 1772, "Pony", OtherSpeciesId, 419977, 5610, 302, "palomino-grey"),
        ["pony_m.adr"] = Pet(17843, 1773, "Pony", OtherSpeciesId, 419983, 5612, 309, "blacktip-brown"),
        ["pony_pegasus_m_fire.adr"] = Pet(78070, 3432, "Fire Pegasus", OtherSpeciesId, 435559, 7080, 290, "dyetint"),
        ["raptor_m.adr"] = Pet(17857, 1769, "Screech", OtherSpeciesId, 4714, 5942, 407, "dark-purple"),
        ["rabbit.adr"] = Pet(7888, 403, "Rabbit", OtherSpeciesId, 4231, 6086, 512, "rabbit-brown"),
        ["reindeer_m.adr"] = Pet(76534, 1934, "Yule", OtherSpeciesId, 5057, 5614, 341, "basic-white"),
        ["reindeer_m_rednose.adr"] = Pet(76535, 1968, "Garnet", OtherSpeciesId, 5059, 5615, 0, "dyetint"),
        ["snake.adr"] = Pet(7890, 180, "Snake", OtherSpeciesId, 4259, 6088, 0, ""),
        ["snowhog.adr"] = Pet(17557, 138, "Snow Hog", OtherSpeciesId, 118985, 6983, 0, ""),
        ["soccerball_m.adr"] = Pet(10322, 3027, "Soccer Ball", OtherSpeciesId, 5630, 6622, 0, ""),
        ["spider_basic.adr"] = Pet(4324, 1924, "Spider", OtherSpeciesId, 4698, 5944, 343, "furry-red"),
        ["spider_m.adr"] = Pet(4324, 1924, "Spider", OtherSpeciesId, 4698, 5944, 343, "furry-red"),
        ["sprite_basic_f.adr"] = Pet(10408, 458, "Fairy Girl", OtherSpeciesId, 6416, 6857, 0, ""),
        ["sprite_basic_m.adr"] = Pet(10407, 454, "Fairy Boy", OtherSpeciesId, 6415, 6860, 0, ""),
        ["tiger_m_lava.adr"] = Pet(76906, 3479, "Lava Tiger", OtherSpeciesId, 6369, 7058, 0, "dyetint"),
        ["tiger_m.adr"] = Pet(17864, 2029, "Tiger", OtherSpeciesId, 4857, 6066, 437, "tiger-orange"),
        ["treeble.adr"] = Pet(9049, 2008, "Treeble", OtherSpeciesId, 5427, 6479, 542, "treebleorange"),
        ["triceratops_m_basic.adr"] = Pet(17867, 1760, "Iggy", OtherSpeciesId, 4560, 5617, 368, "triceratops-green"),
        ["turtle_01.adr"] = Pet(11614, 6, "Turtle", OtherSpeciesId, 170, 6091, 0, "dyetint"),
        ["tyrannosaurus_m.adr"] = Pet(70447, 1759, "T-Rex", OtherSpeciesId, 420571, 5857, 387, "trex-barndoor"),
        ["unicorn_f.adr"] = Pet(17868, 1774, "Unicorn", OtherSpeciesId, 419955, 5618, 324, "basic-white"),
        ["wolf_generic.adr"] = Pet(15493, 3284, "Wolf", OtherSpeciesId, 6270, 6873, 539, "base_grey")
    };

    public static bool TryGet(ClientItemDefinition itemDefinition, out PetDefinitionMetadata metadata)
    {
        if (ItemDefinitions.TryGetValue(itemDefinition.Id, out metadata!))
        {
            metadata = metadata.WithClientItem(itemDefinition);
            return true;
        }

        if (itemDefinition.Type == 2 &&
            !string.IsNullOrWhiteSpace(itemDefinition.ModelName) &&
            ModelNames.TryGetValue(itemDefinition.ModelName, out metadata!))
        {
            metadata = metadata.WithClientItem(itemDefinition);
            return true;
        }

        metadata = null!;
        return false;
    }

    private static PetDefinitionMetadata Pet(int itemDefinitionId, int modelId, string name, int speciesId, int nameId, int imageSetId, int tintId, string tintAlias)
        => new(itemDefinitionId, modelId, name, speciesId, nameId, imageSetId, tintId, tintAlias);

}
