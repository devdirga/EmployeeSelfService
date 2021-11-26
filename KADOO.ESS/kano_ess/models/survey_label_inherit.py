from typing import re
from xml import etree

from odoo import _, models, fields, api
from odoo.exceptions import ValidationError


class SurveyLabelInherit(models.Model):
    _inherit = 'survey.label'
    value2 = fields.Char(string="To")

