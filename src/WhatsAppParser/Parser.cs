using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace WhatsAppParser;

public class Parser {
    private readonly string _filePath;
    private Regex _messageRegex;
    private string _dateFormat;

    public Parser(string filePath, WhatsAppLanguage language) {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ConfigureLanguage(language);
    }

    public IEnumerable<Message> Messages() {
        using var reader = File.OpenText(_filePath);
        MessageBuilder messageBuilder = null;

        while (reader.NextLine(out string line)) {
            var match = _messageRegex.Match(line);
            if (match.Success) {
                // Line starts a new message
                if (messageBuilder != null)
                    // Complete current message and return
                    yield return messageBuilder.Build();

                if (!match.Groups[3].Success) {
                    // No ': ' after senders name.
                    // Probably some encryption message or similar. Just skip this one.
                    messageBuilder = null;
                    continue;
                }

                // Prepare for next message
                var date = match.Groups[1].Value?.Replace('\u202F', ' '); //normalize non-breaking whitespace
                var timestamp = DateTime.ParseExact(date, _dateFormat, CultureInfo.InvariantCulture);
                messageBuilder = new MessageBuilder(timestamp, match.Groups[2].Value);
                messageBuilder.AppendContentLine(match.Groups[4].Value);
            }
            else
                // Line appends to the existing message
                // There might be the case that this line is just invalid and there is no builder from previous lines
                messageBuilder?.AppendContentLine(line);
        }

        if (messageBuilder != null) yield return messageBuilder.Build();
    }

    private void ConfigureLanguage(WhatsAppLanguage language) {
        switch (language) {
            case WhatsAppLanguage.English:
                _messageRegex = new Regex(@"^(\d{1,2}\/\d{1,2}\/\d{2}, \d{1,2}:\d{2}\s[AP]M) - ([^:]+)(: )?(.*)?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                _dateFormat = "M/d/yy, h:mm tt";
                break;
            case WhatsAppLanguage.German:
                _messageRegex = new Regex(@"^(\d{2}\.\d{2}\.\d{2}, \d{2}:\d{2}) - ([^:]+)(: )?(.*)?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                _dateFormat = "dd.MM.yy, HH:mm";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }
}
