// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.Goobstation.Shared.Devil.Contract;
using Robust.Shared.ContentPack;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation.Devil;

/// <summary>
/// Solar: devil contracts are read back from their text, so every clause needs a name that still maps to its id.
/// This catches a renamed clause or a missing text before it silently breaks contracts in game,
/// and checks the devil's guidebook pages still parse.
/// </summary>
[TestFixture]
public sealed class DevilClauseTextTest
{
    private static readonly string[] DevilGuidePages =
    [
        "/ServerInfo/_Goobstation/Guidebook/Antagonist/Devil.xml",
        "/ServerInfo/_Goobstation/Guidebook/Antagonist/DevilClauses.xml",
    ];

    [Test]
    public async Task EveryClauseHasReadableText()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var clause in protoMan.EnumeratePrototypes<DevilClausePrototype>())
                {
                    Assert.That(loc.TryGetString($"devil-clause-name-{clause.ID}", out var name),
                        $"Clause {clause.ID} has no devil-clause-name-{clause.ID} text.");

                    if (name != null)
                    {
                        Assert.That(name.Replace(" ", "").Equals(clause.ID, StringComparison.OrdinalIgnoreCase),
                            $"Clause {clause.ID} is named \"{name}\", which the contract parser would not read back as {clause.ID}.");
                    }

                    Assert.That(loc.TryGetString($"devil-clause-desc-{clause.ID}", out _),
                        $"Clause {clause.ID} has no devil-clause-desc-{clause.ID} text for the devil's menu.");
                    Assert.That(loc.TryGetString($"devil-clause-contract-{clause.ID}", out _),
                        $"Clause {clause.ID} has no devil-clause-contract-{clause.ID} text for the signer.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DevilGuidebookPagesParse()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitIdleAsync();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();

        foreach (var page in DevilGuidePages)
        {
            await client.WaitAssertion(() =>
            {
                using var reader = resMan.ContentFileReadText(new(page));
                Assert.That(parser.TryAddMarkup(new Document(), reader.ReadToEnd()), $"Failed to parse {page}");
            });

            await client.WaitRunTicks(1);
        }

        await pair.CleanReturnAsync();
    }
}
