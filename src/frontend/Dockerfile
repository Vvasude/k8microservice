# ---- Stage 1: build the Angular app ----
FROM node:22-alpine AS build
WORKDIR /app

# Install deps first so this layer caches unless package files change
COPY package.json package-lock.json ./
RUN npm ci

# Build the production bundle -> /app/dist/frontend/browser
COPY . .
RUN npm run build

# ---- Stage 2: serve the static files with nginx ----
FROM nginx:1.27-alpine
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
# nginx:alpine already defines CMD ["nginx", "-g", "daemon off;"]
