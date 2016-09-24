﻿namespace VkNet.Utils
{
    using System;
    using System.Net;
    using System.Text;

    using Enums.Filters;
    using Enums.SafetyEnums;
    using Exception;

    /// <summary>
    /// Браузер, через который производится сетевое взаимодействие с ВКонтакте.
    /// Сетевое взаимодействие выполняется с помощью <see cref="HttpWebRequest"/>.
    /// </summary>
    public class Browser : IBrowser
    {
        /// <summary>
        /// Адрес хоста
        /// </summary>
        private string _host;
        /// <summary>
        /// Порт
        /// </summary>
        private int? _port;
        /// <summary>
        /// Логин для прокси-сервера
        /// </summary>
        private string _proxyLogin;
        /// <summary>
        /// Пароль для прокси-сервера
        /// </summary>
        private string _proxyPassword;

        /// <summary>
        /// Получение json по url-адресу
        /// </summary>
        /// <param name="url">Адрес получения json</param>
        /// <returns>Строка в формате json</returns>
        public string GetJson(string url)
        {
            var separatorPosition = url.IndexOf('?');
            var methodUrl = separatorPosition < 0 ? url : url.Substring(0, separatorPosition);
            var parameters = separatorPosition < 0 ? string.Empty : url.Substring(separatorPosition + 1);

            return WebCall.PostCall(methodUrl, parameters, _host, _port, _proxyLogin, _proxyPassword).Response;
        }

#if false
        /// <summary>
        /// Асинхронное получение json по url-адресу
        /// </summary>
        /// <param name="url">Адрес получения json</param>
        /// <returns>Строка в формате json</returns>
        public async Task<string> GetJsonAsync(string url)
        {
            // todo refactor this shit
            var separatorPosition = url.IndexOf('?');
            string methodUrl = separatorPosition < 0 ? url : url.Substring(0, separatorPosition);
            string parameters = separatorPosition < 0 ? string.Empty : url.Substring(separatorPosition + 1);

            return await WebCall.PostCallAsync(url, parameters);
        }
#endif

        /// <summary>
        /// Авторизация на сервере ВК
        /// </summary>
        /// <param name="appId">Идентификатор приложения</param>
        /// <param name="email">Логин - телефон или эл. почта</param>
        /// <param name="password">Пароль</param>
        /// <param name="settings">Уровень доступа приложения</param>
        /// <param name="code">Код двухфакторной авторизации</param>
        /// <param name="captchaSid">Идентификатор капчи</param>
        /// <param name="captchaKey">Текст капчи</param>
        /// <param name="host">Имя узла прокси-сервера.</param>
        /// <param name="port">Номер порта используемого Host.</param>
        /// <param name="proxyLogin">Логин для прокси-сервера.</param>
        /// <param name="proxyPassword">Пароль для прокси-сервера</param>
        /// <returns>Информация об авторизации приложения</returns>
        public VkAuthorization Authorize(ulong appId, string email, string password, Settings settings, Func<string> code = null, long? captchaSid = null, string captchaKey = null,
                                         string host = null, int? port = null, string proxyLogin = null, string proxyPassword = null)
        {
            _host = string.IsNullOrWhiteSpace(host) ? null : host;
            _port = port;
            _proxyLogin = string.IsNullOrWhiteSpace(proxyLogin) ? null : proxyLogin;
            _proxyPassword = string.IsNullOrWhiteSpace(proxyPassword) ? null : proxyPassword;

            var authorizeUrl = CreateAuthorizeUrlFor(appId, settings, Display.Wap);
            var authorizeUrlResult = WebCall.MakeCall(authorizeUrl, host, port, proxyLogin, proxyPassword);

            // Заполнить логин и пароль
            var loginForm = WebForm.From(authorizeUrlResult).WithField("email").FilledWith(email).And().WithField("pass").FilledWith(password);
            if (captchaSid.HasValue)
                loginForm.WithField("captcha_sid").FilledWith(captchaSid.Value.ToString()).WithField("captcha_key").FilledWith(captchaKey);
            var loginFormPostResult = WebCall.Post(loginForm, host, port, proxyLogin, proxyPassword);

            // Заполнить код двухфакторной авторизации
            if (code != null)
            {
                var codeForm = WebForm.From(loginFormPostResult).WithField("code").FilledWith(code());
                loginFormPostResult = WebCall.Post(codeForm, host, port);
            }

            var authorization = VkAuthorization.From(loginFormPostResult.ResponseUrl);
            if (authorization.CaptchaId.HasValue)
                throw new CaptchaNeededException(authorization.CaptchaId.Value, "http://api.vk.com/captcha.php?sid=" + authorization.CaptchaId.Value);
            if (!authorization.IsAuthorizationRequired)
                return authorization;

            // Отправить данные
            var authorizationForm = WebForm.From(loginFormPostResult);
            var authorizationFormPostResult = WebCall.Post(authorizationForm, host, port, proxyLogin, proxyPassword);

            return VkAuthorization.From(authorizationFormPostResult.ResponseUrl);
        }

        /// <summary>
        /// Построить URL для авторизации.
        /// </summary>
        /// <param name="appId">Идентификатор приложения.</param>
        /// <param name="settings">Настройки прав доступа.</param>
        /// <param name="display">Вид окна авторизации.</param>
        /// <returns></returns>
        public static string CreateAuthorizeUrlFor(ulong appId, Settings settings, Display display)
        {
            var builder = new StringBuilder("https://oauth.vk.com/authorize?");

            builder.AppendFormat("client_id={0}&", appId);
            builder.AppendFormat("scope={0}&", settings);
            builder.Append("redirect_uri=https://oauth.vk.com/blank.html&");
            builder.AppendFormat("display={0}&", display);
            builder.Append("response_type=token");

            return builder.ToString();
        }
    }
}