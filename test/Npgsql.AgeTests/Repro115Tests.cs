using Npgsql.Age.Internal;

namespace Npgsql.AgeTests;

/// <summary>
/// Regression tests for pg-age-digitaltwins#115: GenerateAsPart produced an EMPTY
/// column-definition list (rendered as "as ( agtype)" / "as ()") for scalar and partial
/// map-projection return items, which PostgreSQL rejects with
/// 42601 "syntax error at or near )".
/// </summary>
public class Repro115Tests
{
    private static void AssertNotEmptyColumnList(string cypher, string expected)
    {
        var result = CypherHelpers.GenerateAsPart(cypher);
        Assert.NotEqual("()", result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ScalarReturnWithAlias_ProducesColumnList()
    {
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.returnPeriod AS period",
            "(period agtype)"
        );
    }

    [Fact]
    public void PartialMapProjectionWithAlias_ProducesColumnList()
    {
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t { .returnPeriod } AS twin",
            "(twin agtype)"
        );
    }

    [Fact]
    public void ScalarReturnWithAliasAndOrderBy_ProducesColumnList()
    {
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.returnPeriod AS period ORDER BY period DESC LIMIT 2000",
            "(period agtype)"
        );
    }

    [Fact]
    public void ScalarReturnWithoutAlias_ProducesColumnList()
    {
        // Mixed-case property names are quoted - the driver's pre-existing convention
        // (see GenerateAsPart_WithUpperCaseColumnNames).
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.returnPeriod",
            "(\"returnPeriod\" agtype)"
        );
    }

    [Fact]
    public void PartialMapProjectionWithoutAlias_ProducesColumnList()
    {
        // A projection without an alias resolves to the generic "result" column,
        // same as a bare map literal.
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t { .returnPeriod }",
            "(result agtype)"
        );
    }

    [Fact]
    public void MultipleMixedReturnItems_ProduceOneColumnEach()
    {
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t, t { .id } AS twin, t.returnPeriod AS period",
            "(t agtype, twin agtype, period agtype)"
        );
    }

    [Fact]
    public void ProjectionWithStarKeepsExistingBehaviour()
    {
        // Regression guard: the pre-existing ".*" projection shapes must be unchanged.
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t { .* } AS twin",
            "(twin agtype)"
        );
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t { .*, zoomable: false }",
            "(result agtype)"
        );
    }

    [Fact]
    public void ClauseKeywordPrefixedPropertyNamesAreNotTruncated()
    {
        // These property names all start with a clause keyword and used to truncate the
        // capture to "t." / "t { .", yielding an empty column list.
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.limit AS lim",
            "(lim agtype)"
        );
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.order AS o",
            "(o agtype)"
        );
        AssertNotEmptyColumnList(
            "MATCH (t:Twin) RETURN t.skip AS s",
            "(s agtype)"
        );
    }

    [Fact]
    public void ColumnNameIsNeverEmpty_ForAnyReturnItem()
    {
        // "as ()" / "as ( agtype)" is never valid SQL. Whatever the input, every generated
        // column definition must carry a non-blank name.
        string[] queries =
        [
            "MATCH (t:Twin) RETURN t.returnPeriod AS period",
            "MATCH (t:Twin) RETURN t { .returnPeriod } AS twin",
            "MATCH (t:Twin) RETURN t { .returnPeriod }",
            "MATCH (t:Twin) RETURN t.returnPeriod",
            "MATCH (t:Twin) RETURN t.returnPeriod AS period ORDER BY period DESC LIMIT 2000",
            "MATCH (t:Twin) RETURN t, t { .id } AS twin, t.returnPeriod AS period",
            "MATCH (t:Twin) RETURN t.limit AS lim ORDER BY lim LIMIT 10",
        ];

        foreach (var q in queries)
        {
            var asPart = CypherHelpers.GenerateAsPart(q);
            Assert.NotEqual("()", asPart);
            Assert.DoesNotContain("( agtype", asPart);

            foreach (var col in asPart.Trim('(', ')').Split(", "))
            {
                var name = col[..^" agtype".Length].Trim();
                Assert.False(
                    string.IsNullOrWhiteSpace(name),
                    $"Empty column name in '{asPart}' for query '{q}'"
                );
            }
        }
    }
}
