# Development
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Production
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.prod.yml up -d