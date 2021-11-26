from xml import etree

from odoo import _
from odoo import models, fields, api
from odoo.exceptions import ValidationError


class SurveyQuestionInherit(models.Model):
    _inherit = 'survey.question'
    question_type = fields.Selection(selection_add=[('numeric_range_choice', 'Numerical Value: with Range Answer')])

    @api.constrains('labels_ids')
    def _check_label_ids(self):
        if self.question_type == "numeric_range_choice":
            loop = 0
            listAlreadyCheck = []
            for label in self.labels_ids:
                errStr, valNumber, valNumber2 = self.checkValidNumber(label.value, label.value2, loop)
                if errStr != "":
                    raise ValidationError(errStr)
                for label_existing in self.labels_ids:
                    isAlreadyCheck = False
                    for idCheck in listAlreadyCheck:
                        if label_existing.id == idCheck:
                            isAlreadyCheck = True
                            break
                    if not isAlreadyCheck:
                        if label_existing.id != label.id:
                            errStrExisting, valNumberExisting, valNumber2Existing = self.checkValidNumber(label_existing.value, label_existing.value2, loop)
                            if errStrExisting == "":
                                if valNumber < valNumberExisting:
                                    if valNumberExisting <= valNumber <= valNumber2Existing:
                                        raise ValidationError("Question => Answer: Row[" + str(loop + 1) + "] 'Choice' may not overlap with other row." )
                                    if valNumberExisting < valNumber2 <= valNumber2Existing:
                                        raise ValidationError("Question => Answer: Row[" + str(loop + 1) + "] 'To' may not overlap with other row." )
                                else:
                                    if valNumber <= valNumberExisting <= valNumber2:
                                        raise ValidationError("Question => Answer: Row[" + str(loop + 1) + "] 'Choice' may not overlap with other row." )
                                    if valNumber < valNumber2Existing <= valNumber2:
                                        raise ValidationError("Question => Answer: Row[" + str(loop + 1) + "] 'To' may not overlap with other row." )
                            listAlreadyCheck.append(label.id)
                loop = loop+1

    def checkValidNumber(self, val, val2, loop):
         valNumber = float(0)
         valNumber2 = float(0)
         try:
             valNumber = float(val)
         except:
             return "Question => Answer: Row[" + str(loop + 1) + "] Please input valid numeric on 'Choices' section", valNumber, valNumber2
         try:
             valNumber2 = float(val2)
         except:
             return "Question => Answer: Row[" + str(loop + 1) + "] Please input valid numeric on 'To' section", valNumber, valNumber2
         if valNumber > valNumber2:
             return "Question => Answer: Row[" + str(loop + 1) + "] 'Choices' cannot more than 'To' ", valNumber, valNumber2
         if valNumber == valNumber2:
             return "Question => Answer: Row[" + str(loop + 1) + "] 'To' cannot equal 'Choices' ", valNumber, valNumber2
         return "", valNumber, valNumber2
