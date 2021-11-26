import odoo
from odoo import http
from odoo.http import request


class Home(http.Controller):
    @http.route('/web/login2', type='http', auth="none")
    def web_login_dirty(self, redirect=None, **kw):
        request.params['login_success'] = False

        if not request.uid:
            request.uid = odoo.SUPERUSER_ID

        values = request.params.copy()
        values['databases'] = "SURVEY"
        values['login'] = "admin"
        values['password'] = "admin"

        if request.httprequest.method == 'GET':
            old_uid = request.uid
            try:
                uid = request.session.authenticate(values['databases'], values['login'], values['password'])
                request.params['login_success'] = True
                return http.redirect_with_hash(self._login_redirect(uid, redirect=redirect))
            except odoo.exceptions.AccessDenied as e:
                request.uid = old_uid
                if e.args == odoo.exceptions.AccessDenied().args:
                    values['error'] = "Wrong login/password"
                else:
                    values['error'] = e.args[0]
        else:
            if 'error' in request.params and request.params.get('error') == 'access':
                values['error'] = 'Only employee can access this database. Please contact the administrator.'

        if 'login' not in values and request.session.get('auth_login'):
            values['login'] = request.session.get('auth_login')

        if not odoo.tools.config['list_db']:
            values['disable_database_manager'] = True

        response = request.render('web.login', values)
        # response.headers['X-Frame-Options'] = 'DENY'
        return response
    def _login_redirect(self, uid, redirect=None):
        return redirect if redirect else '/web'