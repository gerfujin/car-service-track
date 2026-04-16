import { createI18n } from 'vue-i18n'
import en from './en'
import et from './et'

const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('cst_locale') || 'en',
  fallbackLocale: 'en',
  messages: {
    en,
    et
  }
})

export default i18n
