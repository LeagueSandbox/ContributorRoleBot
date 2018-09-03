from controbot.main import log


def test_log(capsys):
    log("test")
    captured = capsys.readouterr()
    assert captured.out == "test\n"
    log("test", end="kek")
    captured = capsys.readouterr()
    assert captured.out == "testkek"
