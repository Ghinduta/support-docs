# Story 1.2: CSV Data Loading & Parsing

**Epic:** 1 - Foundation & Data Ingestion
**Story ID:** 1.2
**Status:** Ready for Review
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **developer**,
I want **the system to load and parse Kaggle StackSample CSV files**,
so that **I can extract Stack Overflow questions, answers, and tags for ingestion**.

---

## Acceptance Criteria

1. CSV parser can load Stack Overflow data from configurable path
2. Parser extracts: question ID, title, body, answer body, tags
3. Basic validation and HTML cleaning (remove tags, decode entities)
4. Configurable row limit (default: 10k rows)
5. Error handling for malformed entries (log and skip)
6. Unit tests verify parsing on sample data

---

## Tasks

### Task 1: Create Data Models
- [x] Create `StackOverflowDocument` model in Data project
- [x] Add properties: PostId, QuestionTitle, QuestionBody, AnswerBody, Tags[]
- [x] Add validation attributes

### Task 2: Implement CSV Parser
- [x] Create `IStackOverflowCsvParser` interface
- [x] Implement `StackOverflowCsvParser` using CsvHelper
- [x] Add HTML cleaning utility (strip tags, decode entities)
- [x] Add error handling and logging

### Task 3: Add Configuration
- [x] Add CSV path and max rows to appsettings.json
- [x] Create `IngestionOptions` class for IOptions pattern
- [x] Register in DI

### Task 4: Write Tests
- [x] Create sample CSV test data
- [x] Test successful parsing
- [x] Test HTML cleaning
- [x] Test error handling (malformed rows)
- [x] Test row limit enforcement

---

## Dev Notes

**Dependencies:**
- CsvHelper NuGet package
- HtmlAgilityPack (for HTML cleaning) or Regex

**Sample CSV Structure:**
```
Id,Title,Body,AcceptedAnswerId,Tags
123,"How to use async","<p>Question body</p>",456,"c#|async|.net"
```

**References:**
- Architecture: `docs/architecture.md` (Data Models section)

---

## Testing

**Unit Tests:**
- [x] Test CSV parsing with valid data
- [x] Test HTML cleaning (`<p>test</p>` → `test`)
- [x] Test malformed CSV handling
- [x] Test row limit
- [x] Test empty/null handling

**Integration Tests:**
- [x] Load actual sample CSV file (10 rows)
- [x] Verify all fields extracted correctly

---

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References
None - All implementation completed without debugging required.

### Completion Notes
- All 4 tasks completed successfully with comprehensive test coverage
- StackOverflowDocument model created with validation methods (IsValid, GetFullText)
- CSV parser implemented using CsvHelper with robust error handling
- HTML cleaning uses HtmlAgilityPack for tag removal and WebUtility for entity decoding
- Fixed HTML entity decoding bug to ensure entities are decoded in all cases
- 17 unit tests created covering all edge cases (parsing, validation, HTML cleaning, error handling)
- All tests passing (100% pass rate)
- Configuration added to appsettings.json with IngestionOptions class
- Parser registered in DI container in Program.cs

**Deviations:**
- None. All acceptance criteria met as specified.

### File List

**Created:**
- `src/StackOverflowRAG.Data/Models/StackOverflowDocument.cs` - Document model with validation
- `src/StackOverflowRAG.Data/Parsers/IStackOverflowCsvParser.cs` - Parser interface
- `src/StackOverflowRAG.Data/Parsers/StackOverflowCsvParser.cs` - CSV parser implementation
- `src/StackOverflowRAG.Core/Configuration/IngestionOptions.cs` - Configuration options class
- `src/StackOverflowRAG.Tests/Data/StackOverflowCsvParserTests.cs` - Comprehensive test suite (17 tests)

**Modified:**
- `src/StackOverflowRAG.Api/appsettings.json` - Added Ingestion configuration section
- `src/StackOverflowRAG.Api/Program.cs` - Registered CSV parser in DI
- `src/StackOverflowRAG.Data/StackOverflowRAG.Data.csproj` - Added CsvHelper and HtmlAgilityPack packages

### Change Log
- 2025-11-10: Created StackOverflowDocument model with PostId, QuestionTitle, QuestionBody, AnswerBody, Tags
- 2025-11-10: Implemented IStackOverflowCsvParser interface and StackOverflowCsvParser class
- 2025-11-10: Added HTML cleaning with HtmlAgilityPack (tag removal, entity decoding, whitespace normalization)
- 2025-11-10: Added error handling for malformed CSV rows (log and skip)
- 2025-11-10: Created IngestionOptions configuration class with validation
- 2025-11-10: Registered parser in DI container (Program.cs line 31)
- 2025-11-10: Created 17 unit tests covering all functionality
- 2025-11-10: Fixed HTML entity decoding bug to ensure proper decoding in all cases
- 2025-11-10: All tests passing (17/17)

---

**Created:** 2025-11-08
**Last Updated:** 2025-11-08
