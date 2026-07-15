using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlbaWorld.Runtime;

public enum AlbaLanguage { PortugueseBrazil, English }

public sealed class LanguageService
{
    private readonly Dictionary<string, string> _pt = new()
    {
        ["app.title"] = "Alba World", ["app.subtitle"] = "Crie, combine e imagine!",
        ["menu.avatar"] = "Meu personagem", ["menu.pet"] = "Meu pet", ["menu.house"] = "Minha casa",
        ["menu.photo"] = "Modo foto", ["menu.settings"] = "Configurações", ["menu.back"] = "Voltar",
        ["credits.kenney"] = "Kenney — www.kenney.nl",
        ["avatar.title"] = "Estilo do personagem", ["pet.title"] = "Escolha seu pet", ["house.title"] = "Decore sua casa",
        ["photo.title"] = "Sua cena está pronta!", ["photo.save"] = "Salvar imagem", ["photo.saved"] = "Imagem salva!",
        ["photo.offline"] = "A imagem ficará salva no aparelho.", ["room.sunny"] = "Quarto ensolarado",
        ["room.cozy"] = "Sala aconchegante", ["common.free"] = "Grátis", ["common.watch"] = "Ver vídeo",
        ["common.choose"] = "Escolher", ["common.language"] = "Idioma", ["language.pt"] = "Português",
        ["language.en"] = "English", ["reward.limit"] = "Você já ganhou os dois presentes de hoje.",
        ["reward.unavailable"] = "O vídeo está indisponível agora. Você pode continuar brincando!",
        ["item.hair.sunny"] = "Cachos de sol", ["item.hair.bubble"] = "Coque bolha", ["item.hair.rainbow"] = "Arco-íris", ["item.hair.cloud"] = "Nuvem", ["item.hair.mint"] = "Menta",
        ["item.outfit.pink"] = "Vestido rosa", ["item.outfit.mint"] = "Conjunto menta", ["item.outfit.blue"] = "Moletom azul", ["item.outfit.sun"] = "Sol", ["item.outfit.lilac"] = "Lilás",
        ["shoes.sun"] = "Tênis sol", ["shoes.mint"] = "Tênis menta", ["shoes.rose"] = "Tênis rosa",
        ["accessory.star"] = "Estrela", ["accessory.flower"] = "Flor", ["accessory.glasses"] = "Óculos", ["accessory.ribbon"] = "Fita",
        ["item.pet.cat"] = "Gatinho", ["item.pet.dog"] = "Cachorrinho", ["item.pet.beaver"] = "Castor", ["item.pet.bee"] = "Abelha", ["item.pet.bunny"] = "Coelhinho", ["item.pet.caterpillar"] = "Lagartinha", ["item.pet.chick"] = "Pintinho", ["item.pet.cow"] = "Vaquinha", ["item.pet.crab"] = "Caranguejo", ["item.pet.deer"] = "Veado", ["item.pet.elephant"] = "Elefante", ["item.pet.fish"] = "Peixinho", ["item.pet.fox"] = "Raposa", ["item.pet.giraffe"] = "Girafa", ["item.pet.hog"] = "Porquinho", ["item.pet.koala"] = "Coala", ["item.pet.lion"] = "Leão", ["item.pet.monkey"] = "Macaquinho", ["item.pet.panda"] = "Panda", ["item.pet.parrot"] = "Papagaio", ["item.pet.penguin"] = "Pinguim", ["item.pet.pig"] = "Porquinho", ["item.pet.polar"] = "Urso polar", ["item.pet.tiger"] = "Tigre", ["item.pet.bow"] = "Laço", ["item.pet.cap"] = "Boné", ["item.pet.bandana"] = "Bandana",
        ["item.furniture.bed"] = "Cama", ["item.furniture.sofa"] = "Sofá", ["item.furniture.table"] = "Mesa", ["item.furniture.plant"] = "Plantinha",
        ["item.furniture.lamp"] = "Luminária", ["item.furniture.rug"] = "Tapete", ["item.furniture.book"] = "Livros", ["item.furniture.picture"] = "Quadro", ["item.furniture.chair"] = "Cadeira", ["item.furniture.shelf"] = "Estante", ["item.furniture.cushion"] = "Almofada", ["item.furniture.clock"] = "Relógio"
    };

    private readonly Dictionary<string, string> _en = new()
    {
        ["app.title"] = "Alba World", ["app.subtitle"] = "Create, mix and imagine!",
        ["menu.avatar"] = "My character", ["menu.pet"] = "My pet", ["menu.house"] = "My home",
        ["menu.photo"] = "Photo mode", ["menu.settings"] = "Settings", ["menu.back"] = "Back",
        ["credits.kenney"] = "Kenney — www.kenney.nl",
        ["avatar.title"] = "Character style", ["pet.title"] = "Choose your pet", ["house.title"] = "Decorate your home",
        ["photo.title"] = "Your scene is ready!", ["photo.save"] = "Save image", ["photo.saved"] = "Image saved!",
        ["photo.offline"] = "The image will stay on this device.", ["room.sunny"] = "Sunny bedroom",
        ["room.cozy"] = "Cozy living room", ["common.free"] = "Free", ["common.watch"] = "Watch video",
        ["common.choose"] = "Choose", ["common.language"] = "Language", ["language.pt"] = "Português",
        ["language.en"] = "English", ["reward.limit"] = "You already earned today's two gifts.",
        ["reward.unavailable"] = "The video is unavailable right now. You can keep playing!",
        ["item.hair.sunny"] = "Sunny curls", ["item.hair.bubble"] = "Bubble bun", ["item.hair.rainbow"] = "Rainbow", ["item.hair.cloud"] = "Cloud", ["item.hair.mint"] = "Mint",
        ["item.outfit.pink"] = "Pink dress", ["item.outfit.mint"] = "Mint set", ["item.outfit.blue"] = "Blue hoodie", ["item.outfit.sun"] = "Sun outfit", ["item.outfit.lilac"] = "Lilac outfit",
        ["shoes.sun"] = "Sun sneakers", ["shoes.mint"] = "Mint sneakers", ["shoes.rose"] = "Rose sneakers",
        ["accessory.star"] = "Star", ["accessory.flower"] = "Flower", ["accessory.glasses"] = "Glasses", ["accessory.ribbon"] = "Ribbon",
        ["item.pet.cat"] = "Kitten", ["item.pet.dog"] = "Puppy", ["item.pet.beaver"] = "Beaver", ["item.pet.bee"] = "Bee", ["item.pet.bunny"] = "Bunny", ["item.pet.caterpillar"] = "Caterpillar", ["item.pet.chick"] = "Chick", ["item.pet.cow"] = "Cow", ["item.pet.crab"] = "Crab", ["item.pet.deer"] = "Deer", ["item.pet.elephant"] = "Elephant", ["item.pet.fish"] = "Fish", ["item.pet.fox"] = "Fox", ["item.pet.giraffe"] = "Giraffe", ["item.pet.hog"] = "Hog", ["item.pet.koala"] = "Koala", ["item.pet.lion"] = "Lion", ["item.pet.monkey"] = "Monkey", ["item.pet.panda"] = "Panda", ["item.pet.parrot"] = "Parrot", ["item.pet.penguin"] = "Penguin", ["item.pet.pig"] = "Pig", ["item.pet.polar"] = "Polar bear", ["item.pet.tiger"] = "Tiger", ["item.pet.bow"] = "Bow", ["item.pet.cap"] = "Cap", ["item.pet.bandana"] = "Bandana",
        ["item.furniture.bed"] = "Bed", ["item.furniture.sofa"] = "Sofa", ["item.furniture.table"] = "Table", ["item.furniture.plant"] = "Plant",
        ["item.furniture.lamp"] = "Lamp", ["item.furniture.rug"] = "Rug", ["item.furniture.book"] = "Books", ["item.furniture.picture"] = "Picture", ["item.furniture.chair"] = "Chair", ["item.furniture.shelf"] = "Shelf", ["item.furniture.cushion"] = "Cushion", ["item.furniture.clock"] = "Clock"
    };

    public AlbaLanguage Current { get; private set; }
    public event Action? Changed;

    public LanguageService(string code = "pt-BR") => Set(code);

    public void Set(string code)
    {
        Current = string.Equals(code, "en", StringComparison.OrdinalIgnoreCase) ? AlbaLanguage.English : AlbaLanguage.PortugueseBrazil;
        Changed?.Invoke();
    }

    public string Code => Current == AlbaLanguage.English ? "en" : "pt-BR";

    public string Get(string key) => (Current == AlbaLanguage.English ? _en : _pt).TryGetValue(key, out var value) ? value : key;
}
