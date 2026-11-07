define("seositemaps/Editor", [
    "dojo/_base/declare",
    "dijit/_Widget",
    "dijit/_TemplatedMixin",
    "dijit/_WidgetsInTemplateMixin",
    "dojox/xml/DomParser",
    "dojo/text!./templates/SeoSitemapProperty.html",
    "epi-cms/contentediting/editors/SelectionEditor",
    "epi/shell/widget/CheckBox"
],
    function (
        declare,
        _Widget,
        _TempateMixing,
        _WidgetsInTemplateMixin,
        domParser,
        template
    ) {

        return declare(
            [_Widget, _TempateMixing, _WidgetsInTemplateMixin],
            {
                templateString: template,

                // SeoSitemapEditorDescriptor replaces these with translated labels.
                labels: {
                    enabled: "Enabled",
                    changefrequency: "Change frequency",
                    priority: "Priority",
                    always: "Always",
                    hourly: "Hourly",
                    daily: "Daily",
                    weekly: "Weekly",
                    monthly: "Monthly",
                    yearly: "Yearly",
                    never: "Never",
                    low: "Low",
                    medium: "Medium",
                    mediumhigh: "Medium-High",
                    high: "High"
                },

                postCreate: function () {
                    this.inherited(arguments);
                    this.frequencySelect.set("selections", this._getfrequencySelections());
                    this.prioritySelect.set("selections", this._getPrioritySelections());

                    // A value passed to the constructor was set before the selects had their options, and without
                    // a value the fields haven't been shown yet.
                    this.set("value", this.value);
                },

                _setReadOnlyAttr: function (value) {
                    this._set("readOnly", value);
                    this.enabledCheckbox.set("readOnly", value);
                    this.frequencySelect.set("readOnly", value);
                    this.prioritySelect.set("readOnly", value);
                },

                _getfrequencySelections: function () {
                    return [
                        { value: "always", text: this.labels.always },
                        { value: "hourly", text: this.labels.hourly },
                        { value: "daily", text: this.labels.daily },
                        { value: "weekly", text: this.labels.weekly },
                        { value: "monthly", text: this.labels.monthly },
                        { value: "yearly", text: this.labels.yearly },
                        { value: "never", text: this.labels.never }
                    ];
                },

                _getPrioritySelections: function () {
                    return [
                        { value: "0.0", text: this.labels.low + " (0.0)" },
                        { value: "0.1", text: "0.1" },
                        { value: "0.2", text: "0.2" },
                        { value: "0.25", text: this.labels.low + " (0.25)" },
                        { value: "0.3", text: "0.3" },
                        { value: "0.4", text: "0.4" },
                        { value: "0.5", text: this.labels.medium + " (0.5)" },
                        { value: "0.6", text: "0.6" },
                        { value: "0.7", text: "0.7" },
                        { value: "0.75", text: this.labels.mediumhigh + " (0.75)" },
                        { value: "0.8", text: "0.8" },
                        { value: "0.9", text: "0.9" },
                        { value: "1.0", text: this.labels.high + " (1.0)" }
                    ];
                },

                _priority: "0.5",
                _frequency: "weekly",
                _enabled: true,

                // Extension point: the CMS connects to this, but not in every view that renders the editor.
                onChange: function (value) {
                },

                _setValueAttr: function (value) {

                    if (value) {
                        var jsDom = domParser.parse(value);

                        var enabledNode = jsDom.byName("enabled")[0];
                        if (enabledNode.childNodes.length) {
                            this._enabled = enabledNode.childNodes[0].nodeValue.toLowerCase() === "true";
                        }

                        var frequencyNode = jsDom.byName("changefreq")[0];
                        if (frequencyNode.childNodes.length) {
                            this._frequency = frequencyNode.childNodes[0].nodeValue;
                        }

                        var priorityNode = jsDom.byName("priority")[0];
                        if (priorityNode.childNodes.length) {
                            this._priority = priorityNode.childNodes[0].nodeValue;
                        }
                    }
                    // Loading a value isn't an edit, so these must not report a change. The checkbox ignores the false,
                    // and its change event is left to the check in _setField.
                    this.enabledCheckbox.set("value", this._enabled, false);
                    this.frequencySelect.set("value", this._frequency, false);
                    this.prioritySelect.set("value", this._priority, false);
                    this._set('value', value);
                },

                isValid: function () {
                    return true;
                },

                _setXml: function () {

                    this._set('value', "<SEOSitemaps>" +
                        "<enabled>" + this._enabled + "</enabled>" +
                        "<changefreq>" + this._frequency + "</changefreq>" +
                        "<priority>" + this._priority + "</priority>" +
                        "</SEOSitemaps>");
                    this.onChange(this.value);
                },

                _setField: function (name, value) {
                    if (value === this[name]) {
                        return;
                    }

                    this[name] = value;
                    this._setXml();
                },

                _enabledOnChange: function (value) {
                    this._setField("_enabled", value);
                },

                _frequencyOnChange: function (value) {
                    this._setField("_frequency", value);
                },

                _priorityOnChange: function (value) {
                    this._setField("_priority", value);
                }
            });
    }
);