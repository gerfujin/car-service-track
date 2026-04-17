FROM node:20-alpine AS build
WORKDIR /app

COPY client-app/package*.json ./
RUN npm ci

COPY client-app/ .
RUN npm run build

FROM nginx:alpine
COPY client-app/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist /usr/share/nginx/html