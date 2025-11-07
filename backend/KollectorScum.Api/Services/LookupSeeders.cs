using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    public class CountrySeeder : GenericLookupSeeder<Country, CountryJsonDto, CountriesJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CountrySeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<CountrySeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        // Constructor for testing
        public CountrySeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<CountrySeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Countries";
        public override string FileName => "countrys.json";

        protected override List<CountryJsonDto>? ExtractDtosFromContainer(CountriesJsonContainer container)
        {
            return container.Countrys;
        }

        protected override Country MapDtoToEntity(CountryJsonDto dto)
        {
            return new Country
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Country> GetRepository()
        {
            return _unitOfWork.Countries;
        }
    }

    public class StoreSeeder : GenericLookupSeeder<Store, StoreJsonDto, StoresJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoreSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<StoreSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public StoreSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<StoreSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Stores";
        public override string FileName => "stores.json";

        protected override List<StoreJsonDto>? ExtractDtosFromContainer(StoresJsonContainer container)
        {
            return container.Stores;
        }

        protected override Store MapDtoToEntity(StoreJsonDto dto)
        {
            return new Store
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Store> GetRepository()
        {
            return _unitOfWork.Stores;
        }
    }

    public class FormatSeeder : GenericLookupSeeder<Format, FormatJsonDto, FormatsJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public FormatSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<FormatSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public FormatSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<FormatSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Formats";
        public override string FileName => "formats.json";

        protected override List<FormatJsonDto>? ExtractDtosFromContainer(FormatsJsonContainer container)
        {
            return container.Formats;
        }

        protected override Format MapDtoToEntity(FormatJsonDto dto)
        {
            return new Format
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Format> GetRepository()
        {
            return _unitOfWork.Formats;
        }
    }

    public class GenreSeeder : GenericLookupSeeder<Genre, GenreJsonDto, GenresJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GenreSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<GenreSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public GenreSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<GenreSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Genres";
        public override string FileName => "genres.json";

        protected override List<GenreJsonDto>? ExtractDtosFromContainer(GenresJsonContainer container)
        {
            return container.Genres;
        }

        protected override Genre MapDtoToEntity(GenreJsonDto dto)
        {
            return new Genre
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Genre> GetRepository()
        {
            return _unitOfWork.Genres;
        }
    }

    public class LabelSeeder : GenericLookupSeeder<Label, LabelJsonDto, LabelsJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public LabelSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<LabelSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public LabelSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<LabelSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Labels";
        public override string FileName => "labels.json";

        protected override List<LabelJsonDto>? ExtractDtosFromContainer(LabelsJsonContainer container)
        {
            return container.Labels;
        }

        protected override Label MapDtoToEntity(LabelJsonDto dto)
        {
            return new Label
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Label> GetRepository()
        {
            return _unitOfWork.Labels;
        }
    }

    public class ArtistSeeder : GenericLookupSeeder<Artist, ArtistJsonDto, ArtistsJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ArtistSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<ArtistSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public ArtistSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<ArtistSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Artists";
        public override string FileName => "artists.json";

        protected override List<ArtistJsonDto>? ExtractDtosFromContainer(ArtistsJsonContainer container)
        {
            return container.Artists;
        }

        protected override Artist MapDtoToEntity(ArtistJsonDto dto)
        {
            return new Artist
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Artist> GetRepository()
        {
            return _unitOfWork.Artists;
        }
    }

    public class PackagingSeeder : GenericLookupSeeder<Packaging, PackagingJsonDto, PackagingsJsonContainer>
    {
        private readonly IUnitOfWork _unitOfWork;

        public PackagingSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<PackagingSeeder> logger,
            IConfiguration configuration)
            : base(fileReader, unitOfWork, logger, configuration)
        {
            _unitOfWork = unitOfWork;
        }

        public PackagingSeeder(
            IJsonFileReader fileReader,
            IUnitOfWork unitOfWork,
            ILogger<PackagingSeeder> logger,
            string dataPath)
            : base(fileReader, unitOfWork, logger, dataPath)
        {
            _unitOfWork = unitOfWork;
        }

        public override string TableName => "Packagings";
        public override string FileName => "packagings.json";

        protected override List<PackagingJsonDto>? ExtractDtosFromContainer(PackagingsJsonContainer container)
        {
            return container.Packagings;
        }

        protected override Packaging MapDtoToEntity(PackagingJsonDto dto)
        {
            return new Packaging
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        protected override IRepository<Packaging> GetRepository()
        {
            return _unitOfWork.Packagings;
        }
    }
}
