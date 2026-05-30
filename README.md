# InfoTrack Solicitor Search

A .NET 9 Web API + Angular 21 SPA that scrapes conveyancing solicitor listings from
[solicitors.com](https://www.solicitors.com/conveyancing.html) by location.

---

## Architecture

```
src/
  InfoTrack.Core/           domain models and interfaces (no dependencies)
  InfoTrack.Infrastructure/ HTML scraper + in-memory repository
  InfoTrack.Api/            ASP.NET Core Web API
tests/
  InfoTrack.Tests/          xUnit tests
infotrack-client/           Angular 21 SPA
```

The HTML is parsed with a custom tokeniser (`Infrastructure/Scraping/HtmlParser.cs`) — no
third-party parsing libraries.

Results are cached per location for 24 hours. Passing `?refresh=true` forces a re-scrape.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-9.0.16-windows-x64-installer?cid=getdotnetcore)
- [Node.js 20+](https://nodejs.org/)
- Angular CLI 21: `npm install -g @angular/cli`

---

## Running the app

### 1. Start the API

```bash
cd src/InfoTrack.Api
dotnet run
```

API runs on **http://localhost:5200** · Swagger UI at **http://localhost:5200/swagger**

### 2. Start the frontend

```bash
cd infotrack-client
npm install
npm start
```

Open **http://localhost:4200**

The Angular dev server proxies `/api/*` to the .NET API, so no CORS configuration is needed.

---

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/solicitors/locations` | Default location list |
| GET | `/api/solicitors?locations=London&locations=Leeds` | Fetch solicitors (cached) |
| GET | `/api/solicitors?locations=London&refresh=true` | Force re-scrape |
| DELETE | `/api/solicitors/cache/{location}` | Invalidate one location |
| DELETE | `/api/solicitors/cache` | Invalidate all |

---

## Tests

```bash
# .NET (36 tests)
dotnet test

# Angular (6 tests)
cd infotrack-client && npm test
```

---

## Notes

- No database needed — results are held in memory and reset on server restart
- Multi-word locations like "Lytham St Annes" are supported (mapped to `lytham-st-annes`)
- Locations not listed on solicitors.com return an empty result with an explanation rather than an error
