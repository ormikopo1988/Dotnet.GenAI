# Ingestion Pipeline

## Overview

The ingestion pipeline is responsible for converting PDF documents into searchable vector embeddings stored in PostgreSQL pgvector. It runs both at application startup and periodically via a background service.

## Pipeline Flow

```
PDF files (wwwroot/Data/*.pdf)
        |
        v
PDFDirectorySource
  |-- Enumerate *.pdf files
  |-- Compute SourceFileId (filename) and SourceFileVersion (last write time UTC)
  |-- Compare against existing IngestedDocument records
        |
        v
DataIngestor
  |-- EnsureCollectionExistsAsync (chunks + documents)
  |-- Delete chunks for removed/modified documents
  |-- For each new/modified document:
  |     |-- Upsert IngestedDocument metadata
  |     |-- CreateChunksForDocumentAsync:
  |           |-- PdfPig opens the PDF
  |           |-- Per page: NearestNeighbourWordExtractor -> DocstrumBoundingBoxes -> text blocks
  |           |-- Join blocks as paragraphs
  |           |-- TextChunker.SplitPlainTextParagraphs (max 200 tokens)
  |           |-- Create IngestedChunk per chunk (with DocumentId, PageNumber, Text)
  |     |-- Upsert chunks to vector collection
        |
        v
pgvector VectorStoreCollections
  |-- "data-dotnet_genai_mycareerassistant-chunks" (IngestedChunk)
  |-- "data-dotnet_genai_mycareerassistant-documents" (IngestedDocument)
```

## Key Components

### IIngestionSource Interface

Defines the contract for data sources:
- `SourceId` -- unique identifier for the source
- `GetNewOrModifiedDocumentsAsync` -- returns documents that are new or have changed
- `GetDeletedDocumentsAsync` -- returns documents that no longer exist in the source
- `CreateChunksForDocumentAsync` -- splits a document into searchable chunks

### PDFDirectorySource

The default implementation that processes PDF files:
- Tracks document versions using file last-write time (UTC ISO 8601)
- Uses PdfPig for layout-aware text extraction (`NearestNeighbourWordExtractor` + `DocstrumBoundingBoxes`)
- Chunks text with Semantic Kernel's `TextChunker.SplitPlainTextParagraphs` at 200 tokens max
- Each chunk records its source document ID and page number for citation linking

### DataIngestor

Orchestrates the ingestion lifecycle:
- Ensures vector collections exist before processing
- Performs incremental updates: only processes new/modified documents
- Cleans up chunks when documents are modified or deleted
- Has both a static entry point (for startup) and an instance method (for background service)

### DataIngestionService

A `BackgroundService` that periodically re-runs ingestion:
- Interval configured via `BackgroundServiceTimespanMin:DataIngestion` in `appsettings.json` (default: 1 minute)
- Creates a new DI scope per tick to get fresh `DataIngestor` and `PDFDirectorySource` instances

## Vector Store Models

### IngestedDocument
Metadata record for a tracked document:
- `Key` -- GUID v7 string
- `SourceId` -- identifies the ingestion source
- `DocumentId` -- filename (used for citation linking)
- `DocumentVersion` -- last write time for change detection

### IngestedChunk
Individual searchable text chunk:
- `Key` -- GUID v7 string
- `DocumentId` -- source filename
- `PageNumber` -- PDF page number
- `Text` -- chunk text (vector mapping source for embedding generation)

## Extending the Pipeline

To add a new ingestion source:
1. Implement `IIngestionSource`
2. Call `DataIngestor.IngestDataAsync(services, new YourSource(...))` in `Program.cs`
3. Optionally add periodic re-ingestion in `DataIngestionService`
