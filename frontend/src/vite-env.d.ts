/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** URL base da API. Vazio = mesma origem (proxy do Vite em dev, Nginx no Docker). */
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
