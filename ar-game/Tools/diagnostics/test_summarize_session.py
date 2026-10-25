import json
import unittest

from summarize_session import summarize


class SummaryTests(unittest.TestCase):
    def test_reports_first_lock_and_medians(self):
        records = [
            {"t": 1000, "alignment": "Unlocked", "hAcc": 5, "yawAcc": 6},
            {"t": 2000, "alignment": "Locked", "hAcc": 1, "yawAcc": 2},
            {"t": 2500, "alignment": "Locked", "hAcc": 3, "yawAcc": 4},
        ]
        result = summarize(map(json.dumps, records))
        self.assertEqual(result["records"], 3)
        self.assertEqual(result["lockedRecords"], 2)
        self.assertEqual(result["firstLockSeconds"], 1.0)
        self.assertEqual(result["medianLockedHorizontalM"], 2.0)
        self.assertEqual(result["medianLockedYawDeg"], 3.0)

    def test_no_lock_reports_null_metrics(self):
        result = summarize([json.dumps({"t": 1000, "alignment": "Unlocked", "hAcc": None, "yawAcc": None})])
        self.assertEqual(result["lockedRecords"], 0)
        self.assertIsNone(result["firstLockSeconds"])
        self.assertIsNone(result["medianLockedHorizontalM"])

    def test_rejects_unsanitized_or_invalid_json(self):
        for line in ['{"t":1,"lat":0,"alignment":"Unlocked"}', '{"t":NaN}', 'broken']:
            with self.subTest(line=line), self.assertRaises(ValueError):
                summarize([line])


if __name__ == "__main__":
    unittest.main()
