/**
 * Kansas Child Support Worksheet — wizard.js
 * Vanilla JS only. Handles:
 * - Show/hide toggle fields (radio and checkbox)
 * - Live income estimate
 */

(function () {
    'use strict';

    // -----------------------------------------------------------------------
    // Toggle visibility: radio buttons with data-target
    // -----------------------------------------------------------------------
    function setupRadioToggles() {
        document.querySelectorAll('.toggle-radio').forEach(function (radio) {
            radio.addEventListener('change', handleRadioToggle);
        });
        // Run on load for pre-filled values
        document.querySelectorAll('.toggle-radio:checked').forEach(function (radio) {
            handleRadioToggle.call(radio);
        });
    }

    function handleRadioToggle() {
        var targetId = this.getAttribute('data-target');
        if (!targetId) return;
        var target = document.getElementById(targetId);
        if (!target) return;

        // Find all radios in the same name group with same data-target
        var name = this.name;
        var allRadios = document.querySelectorAll('input[name="' + name + '"][data-target="' + targetId + '"]');
        var shouldShow = (this.value === 'true' || this.value === 'Yes');
        target.classList.toggle('hidden', !shouldShow);
    }

    // -----------------------------------------------------------------------
    // Toggle visibility: checkboxes with data-target
    // -----------------------------------------------------------------------
    function setupCheckboxToggles() {
        document.querySelectorAll('.checkbox-toggle').forEach(function (cb) {
            cb.addEventListener('change', handleCheckboxToggle);
        });
        // Run on load
        document.querySelectorAll('.checkbox-toggle').forEach(function (cb) {
            handleCheckboxToggle.call(cb);
        });
    }

    function handleCheckboxToggle() {
        var targetId = this.getAttribute('data-target');
        if (!targetId) return;
        var target = document.getElementById(targetId);
        if (!target) return;
        target.classList.toggle('hidden', !this.checked);
    }

    // -----------------------------------------------------------------------
    // Live income estimate on income pages
    // -----------------------------------------------------------------------
    function setupIncomeEstimate() {
        var estimate = document.getElementById('income-estimate');
        if (!estimate) return;

        document.querySelectorAll('.income-field').forEach(function (field) {
            field.addEventListener('input', updateEstimate);
        });
        document.querySelectorAll('input[name="HasEmploymentIncome"], input[name="IsSelfEmployed"], ' +
            'input[name="HasBonusIncome"], input[name="HasMilitaryPay"], input[name="HasDisabilityPayments"], ' +
            'input[name="HasUnemploymentCompensation"], input[name="HasRetirementIncome"], input[name="HasOtherIncome"]').forEach(function (r) {
            r.addEventListener('change', updateEstimate);
        });
        updateEstimate();
    }

    function parseAmount(val) {
        var cleaned = String(val).replace(/[^0-9.]/g, '');
        var n = parseFloat(cleaned);
        return isNaN(n) ? 0 : n;
    }

    function updateEstimate() {
        var estimate = document.getElementById('income-estimate');
        if (!estimate) return;

        var total = 0;

        function addIfEnabled(cbName, fieldName) {
            var enabled = document.querySelector('input[name="' + cbName + '"]:checked');
            if (!enabled) return;
            if (enabled.value === 'false' || enabled.value === 'No') return;
            var field = document.querySelector('input[name="' + fieldName + '"]');
            if (field) total += parseAmount(field.value);
        }

        addIfEnabled('HasEmploymentIncome', 'MonthlyGrossEmployment');
        // Self-employment net
        var seEnabled = document.querySelector('input[name="IsSelfEmployed"]:checked');
        if (seEnabled && seEnabled.value === 'true') {
            var gross = parseAmount((document.querySelector('input[name="MonthlyGrossSelfEmployment"]') || {}).value);
            var exp = parseAmount((document.querySelector('input[name="MonthlyBusinessExpenses"]') || {}).value);
            total += Math.max(0, gross - exp);
        }

        var checkboxIncomes = ['HasBonusIncome', 'HasMilitaryPay', 'HasDisabilityPayments',
            'HasUnemploymentCompensation', 'HasRetirementIncome', 'HasOtherIncome'];
        var checkboxFields = ['MonthlyBonusAverage', 'MonthlyMilitaryPay', 'MonthlyDisabilityPayments',
            'MonthlyUnemploymentCompensation', 'MonthlyRetirementIncome', 'MonthlyOtherIncome'];

        checkboxIncomes.forEach(function (name, i) {
            var cb = document.querySelector('input[name="' + name + '"]');
            if (cb && cb.checked) {
                var field = document.querySelector('input[name="' + checkboxFields[i] + '"]');
                if (field) total += parseAmount(field.value);
            }
        });

        // Deductions
        var csEnabled = document.querySelector('input[name="PaysChildSupportOtherCases"]:checked');
        if (csEnabled && csEnabled.value === 'true') {
            var csField = document.querySelector('input[name="MonthlyChildSupportPaidOtherCases"]');
            if (csField) total -= parseAmount(csField.value);
        }
        var maintEnabled = document.querySelector('input[name="PaysMaintenancePaid"]:checked');
        if (maintEnabled && maintEnabled.value === 'true') {
            var maintField = document.querySelector('input[name="MonthlyMaintenancePaid"]');
            if (maintField) total -= parseAmount(maintField.value) * 1.25;
        }
        var maintRecEnabled = document.querySelector('input[name="ReceivesMaintenance"]:checked');
        if (maintRecEnabled && maintRecEnabled.value === 'true') {
            var maintRecField = document.querySelector('input[name="MonthlyMaintenanceReceived"]');
            if (maintRecField) total += parseAmount(maintRecField.value);
        }

        total = Math.round(total);
        estimate.textContent = '$' + total.toLocaleString() + '/month';
    }

    // -----------------------------------------------------------------------
    // Auto-scroll to first error on page load
    // -----------------------------------------------------------------------
    function scrollToFirstError() {
        var firstError = document.querySelector('.validation-summary, .field-error');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    // -----------------------------------------------------------------------
    // Init
    // -----------------------------------------------------------------------
    function init() {
        setupRadioToggles();
        setupCheckboxToggles();
        setupIncomeEstimate();
        scrollToFirstError();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
