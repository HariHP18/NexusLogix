# --- STAGE 1: Build React Assets ---
FROM node:22-alpine AS build-env
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm ci

# Copy the source files and build the production bundle
COPY . ./
RUN npm run build

# --- STAGE 2: Serve with Nginx ---
FROM nginx:alpine
# Copy the compiled static assets from the build stage to Nginx's web directory
COPY --from=build-env /app/dist /usr/share/nginx/html

# Expose Nginx standard port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]