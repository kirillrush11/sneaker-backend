# Отчёт по лабораторной работе №3 — Руш Кирилл СГН3-41Б

## Тема: Разработка серверной части веб-приложения агрегатора кроссовок

---

## Архитектура

Приложение построено на трёхслойной архитектуре с чётким разделением ответственности:

```
DataBase (слой данных)  →  Services (бизнес-логика)  →  Controllers (API)
        ↑_________________________Converters_________________________↑
```

---

## Структура проекта

```
SneakerAgregator/
├── Program.cs                              # Точка входа, DI, JWT, Swagger
│
├── Controllers/                            # API-контроллеры
│   ├── AuthController.cs                   # Регистрация и вход
│   ├── FavoritesController.cs              # Избранное пользователя
│   ├── ParserController.cs                 # Запуск парсеров
│   ├── ProductsController.cs               # Каталог кроссовок
│   ├── ProductsDbController.cs             # Административные данные
│   ├── StoresController.cs                 # Магазины
│   └── Models/                             # DTO / ViewModels
│       ├── AuthViewModels.cs               # RegisterRequest, LoginRequest, AuthResponse
│       ├── FavoriteViewModels.cs           # FavoriteDto
│       ├── ProductViewModels.cs            # ProductSummaryDto, ProductDetailDto, CreateProductRequest, UpdateProductRequest
│       └── StoreViewModels.cs              # StoreDto, CreateStoreRequest, UpdateStoreRequest
│
├── Converters/                             # Конвертеры между слоями
│   ├── FromEfToServiceConverter.cs         # Entity → ServiceModel
│   ├── FromServiceToEfConverter.cs         # ServiceModel → Entity
│   ├── FromServiceToViewModelConverter.cs  # ServiceModel → DTO
│   └── FromViewModelToServiceConverter.cs  # DTO → ServiceModel
│
├── DataBase/                               # Слой данных
│   ├── AppDbContext.cs                     # sneakers.db: пользователи, избранное
│   ├── ProductsDbContext.cs                # products.db: магазины, предложения, размеры
│   ├── Extensions.cs                       # AddDatabase() — регистрация контекстов
│   ├── Models/                             # EF-сущности
│   │   ├── Favorite.cs
│   │   ├── Offer.cs
│   │   ├── Product.cs
│   │   ├── SizeAvailability.cs
│   │   ├── Store.cs
│   │   └── User.cs
│   ├── Configurations/                     # Fluent API конфигурации
│   │   ├── FavoriteConfiguration.cs
│   │   ├── OfferConfiguration.cs
│   │   ├── ProductConfiguration.cs
│   │   ├── SizeAvailabilityConfiguration.cs
│   │   ├── StoreConfiguration.cs
│   │   └── UserConfiguration.cs
│   └── Repositories/                       # Репозитории и интерфейсы
│       ├── IFavoriteRepository.cs
│       ├── IOfferRepository.cs
│       ├── IProductRepository.cs
│       ├── IStoreRepository.cs
│       ├── IUserRepository.cs
│       ├── FavoriteRepository.cs
│       ├── OfferRepository.cs
│       ├── ProductRepository.cs
│       ├── StoreRepository.cs
│       └── UserRepository.cs
│
└── Services/                               # Бизнес-логика
    ├── Models/                             # Сервисные модели
    │   ├── FavoriteModel.cs
    │   ├── OfferModel.cs
    │   ├── ProductModel.cs
    │   ├── SizeModel.cs
    │   ├── StoreModel.cs
    │   └── UserModel.cs
    ├── AuthService/
    │   ├── IAuthService.cs
    │   └── AuthService.cs                  # JWT-аутентификация
    ├── FavoriteService/
    │   ├── IFavoriteService.cs
    │   └── FavoriteService.cs
    ├── ProductService/
    │   ├── IProductService.cs
    │   └── ProductService.cs
    └── ParserService/
        ├── StreetBeatParser.cs             # Парсер Street Beat (Playwright)
        └── BrandshopParser.cs              # Парсер Brandshop (Playwright)
```

---

## Базы данных

Приложение использует две SQLite-базы данных:

| База | Файл | Содержимое |
|------|------|------------|
| Основная | `sneakers.db` | Пользователи (`User`), избранное (`Favorite`), продукты (`Product`) |
| Продукты | `products.db` | Магазины (`Store`), предложения (`Offer`), размеры (`SizeAvailability`) |

---

## API эндпоинты

### Auth — `/api/auth`
| Метод | URL | Описание | Коды |
|-------|-----|----------|------|
| POST | `/register` | Регистрация | 200, 400, 409 |
| POST | `/login` | Вход, возвращает JWT | 200, 400, 401 |
| GET | `/me` | Данные текущего пользователя | 200, 401 |

### Products — `/api/products`
| Метод | URL | Описание | Коды |
|-------|-----|----------|------|
| GET | `/` | Каталог с фильтрацией по бренду и полу | 200 |
| GET | `/new` | Новинки | 200 |
| GET | `/brands` | Список брендов | 200 |
| GET | `/search?q=` | Поиск | 200, 400, 404 |
| GET | `/{id}` | Детали товара | 200, 404 |
| GET | `/{id}/sizes` | Доступные размеры | 200, 404 |
| POST | `/` | Создать товар (auth) | 201, 400, 401, 409 |
| PATCH | `/{id}` | Обновить товар (auth) | 200, 400, 401, 404 |
| DELETE | `/{id}` | Удалить товар (auth) | 204, 401, 404 |

### Stores — `/api/stores`
| Метод | URL | Описание | Коды |
|-------|-----|----------|------|
| GET | `/` | Все магазины | 200 |
| GET | `/{id}` | Магазин по ID | 200, 404 |
| GET | `/{id}/products` | Товары магазина | 200, 404 |
| POST | `/` | Создать магазин (auth) | 201, 400, 401, 409 |
| PATCH | `/{id}` | Обновить магазин (auth) | 200, 401, 404 |
| DELETE | `/{id}` | Удалить магазин (auth) | 204, 401, 404 |

### Favorites — `/api/favorites` (требует авторизации)
| Метод | URL | Описание | Коды |
|-------|-----|----------|------|
| GET | `/` | Избранное пользователя | 200, 401 |
| POST | `/{productId}` | Добавить в избранное | 200, 401, 409 |
| DELETE | `/{productId}` | Удалить из избранного | 200, 401, 404 |

### Parser — `/api/parser` (запуск парсеров)
| Метод | URL | Описание | Коды |
|-------|-----|----------|------|
| POST | `/streetbeat` | Парсинг Street Beat | 200, 500 |
| POST | `/brandshop` | Парсинг Brandshop | 200, 500 |
| POST | `/all` | Парсинг всех магазинов | 200, 500 |

---

## Ссылки

- [Controllers/](https://git.iu7.bmstu.ru/rka24s098/internettech3/-/tree/lab3/SneakerAgregator/Controllers)
- [Services/](https://git.iu7.bmstu.ru/rka24s098/internettech3/-/tree/lab3/SneakerAgregator/Services)
- [DataBase/](https://git.iu7.bmstu.ru/rka24s098/internettech3/-/tree/lab3/SneakerAgregator/DataBase)
- [Converters/](https://git.iu7.bmstu.ru/rka24s098/internettech3/-/tree/lab3/SneakerAgregator/Converters)
- [Program.cs](https://git.iu7.bmstu.ru/rka24s098/internettech3/-/blob/lab3/SneakerAgregator/Program.cs)
- [Swagger UI](http://localhost:5001/swagger/index.html)
- [Демонстрация работы (видео 1, 2)](https://disk.yandex.ru/d/Q3bWShHudDNMLg)
- [Схема БД](https://disk.yandex.ru/d/06qEn4YlMpOnag)
