using Ledger.Api.Domain;

namespace Ledger.UnitTests;

public sealed class LedgerEntryTests
{
    [Fact]
    public void EntryType_ShouldExposeDebitAndCredit()
    {
        Enum.IsDefined(typeof(LedgerEntryType), LedgerEntryType.Debit).Should().BeTrue();
        Enum.IsDefined(typeof(LedgerEntryType), LedgerEntryType.Credit).Should().BeTrue();
    }
}
