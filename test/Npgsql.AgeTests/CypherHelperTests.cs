using Npgsql.Age.Internal;

namespace Npgsql.AgeTests
{
    public class CypherHelpersTest
    {
        [Fact]
        public void GenerateAsPart_SingleReturnValue()
        {
            string cypher = "MATCH (n)-[r]->(m) RETURN n";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(n agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_MultipleReturnValues()
        {
            string cypher = "MATCH (n)-[r]->(m) RETURN n, r, m";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(n agtype, r agtype, m agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_ReturnsResultAgtype_WhenNoReturnPart()
        {
            string cypher = "MATCH (n) WHERE n.name = 'Alice'";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_GenerateAsPart_WithSingleAccessor()
        {
            string cypher = "MATCH (n) RETURN n.name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(name agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithMultipleAccessors()
        {
            string cypher = "MATCH (n) RETURN n.name, n.age";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(name agtype, age agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithFunctionCall()
        {
            string cypher = "MATCH (n) RETURN count(n)";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(count agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithAlias()
        {
            string cypher = "MATCH (n) RETURN n.name AS Name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"Name\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithNumbers()
        {
            string cypher = "MATCH (n) RETURN 123, 45.67";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(num agtype, num1 agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithSpecialCharacters()
        {
            string cypher = "MATCH (n) RETURN n.`first-name`, n.`last-name`";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(first_name agtype, last_name agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithDuplicateColumnNames()
        {
            string cypher = "MATCH (n) RETURN n.name, n.name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(name agtype, name1 agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithUpperCaseColumnNames()
        {
            string cypher = "MATCH (n) RETURN n.Name, n.Age";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"Name\" agtype, \"Age\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithLineBreaks()
        {
            string cypher =
                @"
        MATCH(r: Twin { `$dtId`: 'room1' })- [rel:rel_has_sensors]->(s: Twin)
        RETURN r, rel, s
        ";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(r agtype, rel agtype, s agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithLineBreaksAndExtraSpacesAndLimit()
        {
            string cypher =
                @"
        MATCH(r: Twin { `$dtId`: 'room1' })- [rel:rel_has_sensors]->(s: Twin)
        RETURN r,         rel, 
        s
LIMIT 10";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(r agtype, rel agtype, s agtype)", result);
        }

        [Fact]
        public void EscapeCypher_EscapesBackslashes()
        {
            string cypher =
                @"WITH '{""dtId"":""abc"",""name"":""weird\n\\""\'name""'}'::agtype as twin
            MERGE (t: Twin {{`$dtId`: 'abc'}})
            SET t = twin
            RETURN t";
            string result = CypherHelpers.EscapeCypher(cypher);
            Assert.Equal(
                @"WITH '{""dtId"":""abc"",""name"":""weird\\n\\\\""\'name""'}'::agtype as twin
            MERGE (t: Twin {{`$dtId`: 'abc'}})
            SET t = twin
            RETURN t",
                result
            );
        }

        [Fact]
        public void EscapeCypher_HandlesMatchingKeywordsInQuery()
        {
            string cypher =
                @"WITH '{""dtId"":""abc"",""name"":""return\n\\""\'limit""'}'::agtype as twin
            MERGE (t: Twin {{`$dtId`: 'abc'}})
            SET t = twin";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }
    }
}
