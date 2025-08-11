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
        public void GenerateAsPart_WithObjectReturn()
        {
            string cypher = "MATCH (n) RETURN {name: n.name, age: n.age}";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithObjectReturnWithAlias()
        {
            string cypher = "MATCH (n) RETURN {name: n.name, age: n.age} AS person";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(person agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithArrayReturn()
        {
            string cypher = "MATCH (n) RETURN [n.name, n.age]";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithCombinedAliasedReturn()
        {
            string cypher =
                "MATCH (n) RETURN [n.name, n.age] AS personArray, {name: n.name, age: n.age} AS personObject, n.name AS name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(personArray agtype, personObject agtype, name agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithFunctionCall()
        {
            string cypher = "MATCH (n) RETURN count(n)";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(count agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithOrderBy()
        {
            string cypher = "MATCH (n) RETURN n ORDER BY n.name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(n agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithAlias()
        {
            string cypher = "MATCH (n) RETURN n.name AS Name";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"Name\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithSquareBracketColumnNamesAndAlias()
        {
            string cypher = "MATCH (n) RETURN n['test'] AS Name";
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
        public void GenerateAsPart_WithSquareBracketColumnNames()
        {
            string cypher = "MATCH (n) RETURN n['$id'], n['name'], n['age']";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"$id\" agtype, \"name\" agtype, \"age\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithUpperCaseColumnNames()
        {
            string cypher = "MATCH (n) RETURN n.Name, n.Age";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"Name\" agtype, \"Age\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_WithMixedReturns()
        {
            string cypher = "MATCH (n) RETURN n['id'] AS Name, 123, n.age AS Age, n['email']";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"Name\" agtype, num agtype, \"Age\" agtype, \"email\" agtype)", result);
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
        public void GenerateAsPart_HandlesMatchingKeywordsInQuery()
        {
            string cypher =
                @"WITH '{""dtId"":""abc"",""name"":""return\n\\""\'limit""'}'::agtype as twin
            MERGE (t: Twin {{`$dtId`: 'abc'}})
            SET t = twin";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_HandlesComplexUnwindQuery()
        {
            string cypher =
                """UNWIND ['{\"id\":\"dtmi:com:arcadis:app:Query;2\",\"model\":{\"@id\":\"dtmi:com:arcadis:app:Query;2\",\"@type\":\"Interface\",\"displayName\":\"Query\",\"@context\":[\"dtmi:dtdl:context;3\",\"dtmi:dtdl:extension:quantitativeTypes;1\"],\"contents\":[{\"@type\":\"Property\",\"name\":\"twinId\",\"displayName\":{\"en\":\"Twin ID\"},\"description\":\"The related twin Id to fetch viewer data\",\"schema\":\"string\",\"comment\":\"category:Configuration;twinSelectSingle\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"name\",\"displayName\":{\"en\":\"Name\"},\"schema\":\"string\",\"comment\":\"category:Configuration\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"queryKind\",\"displayName\":{\"en\":\"Query type\"},\"schema\":{\"@type\":\"Enum\",\"valueSchema\":\"string\",\"enumValues\":[{\"name\":\"adt\",\"enumValue\":\"adt\",\"displayName\":{\"en\":\"Azure Digital Twins (SQL)\"},\"description\":\"A static ADT query, or a JEXL expression preceded by an equal sign that evaluates to a valid ADT query.\"},{\"name\":\"adx\",\"enumValue\":\"adx\",\"displayName\":{\"en\":\"Azure Data Explorer (KQL)\"},\"description\":\"Parameters are can be used in the query using the variable name preceded by an underscore.\"}]},\"description\":{\"en\":\"The query that should return full twins.\"},\"comment\":\"category:Configuration\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"query\",\"displayName\":{\"en\":\"Query\"},\"comment\":\"category:Configuration;textarea\",\"schema\":\"string\",\"writable\":true}]},\"uploadTime\":\"2025-02-11T07:23:06.2912676+00:00\",\"displayName\":{\"en\":\"Query\"},\"description\":{},\"decommissioned\":false}'] as model\n WITH model::agtype as modelAgtype\n CREATE (m:Model {id: modelAgtype['id']})\n SET m = modelAgtype\n RETURN m""";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(m agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_HandlesComplexUnwindQueryWithoutReturn()
        {
            string cypher =
                """UNWIND ['{\"id\":\"dtmi:com:arcadis:app:Query;2\",\"model\":{\"@id\":\"dtmi:com:arcadis:app:Query;2\",\"@type\":\"Interface\",\"displayName\":\"Query\",\"@context\":[\"dtmi:dtdl:context;3\",\"dtmi:dtdl:extension:quantitativeTypes;1\"],\"contents\":[{\"@type\":\"Property\",\"name\":\"twinId\",\"displayName\":{\"en\":\"Twin ID\"},\"description\":\"The related twin Id to fetch viewer data\",\"schema\":\"string\",\"comment\":\"category:Configuration;twinSelectSingle\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"name\",\"displayName\":{\"en\":\"Name\"},\"schema\":\"string\",\"comment\":\"category:Configuration\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"queryKind\",\"displayName\":{\"en\":\"Query type\"},\"schema\":{\"@type\":\"Enum\",\"valueSchema\":\"string\",\"enumValues\":[{\"name\":\"adt\",\"enumValue\":\"adt\",\"displayName\":{\"en\":\"Azure Digital Twins (SQL)\"},\"description\":\"A static ADT query, or a JEXL expression preceded by an equal sign that evaluates to a valid ADT query.\"},{\"name\":\"adx\",\"enumValue\":\"adx\",\"displayName\":{\"en\":\"Azure Data Explorer (KQL)\"},\"description\":\"Parameters are can be used in the query using the variable name preceded by an underscore.\"}]},\"description\":{\"en\":\"The query that should return full twins.\"},\"comment\":\"category:Configuration\",\"writable\":true},{\"@type\":\"Property\",\"name\":\"query\",\"displayName\":{\"en\":\"Query\"},\"comment\":\"category:Configuration;textarea\",\"schema\":\"string\",\"writable\":true}]},\"uploadTime\":\"2025-02-11T07:23:06.2912676+00:00\",\"displayName\":{\"en\":\"Query\"},\"description\":{},\"decommissioned\":false}'] as model\n WITH model::agtype as modelAgtype\n CREATE (m:Model {id: modelAgtype['id']})\n SET m = modelAgtype""";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(result agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_HansdlesFunctionsInReturn()
        {
            string cypher = "MATCH (n) RETURN count(n) AS totalCount";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"totalCount\" agtype)", result);
        }

        [Fact]
        public void GenerateAsPart_HansdlesFunctionsInReturn2()
        {
            string cypher = "MATCH (n) RETURN coalesce(n.name,'User') AS userName";
            string result = CypherHelpers.GenerateAsPart(cypher);
            Assert.Equal("(\"userName\" agtype)", result);
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
    }
}
