<?php

header("Access-Control-Allow-Origin: http://localhost:3000");
header('Access-Control-Allow-Methods: GET, POST');
header("Access-Control-Allow-Headers: *");

// require_once 'service/bflib.php';
session_id('bf-sg-session');
session_start();

// unset($_SESSION['user']['id']);
// return;

mb_internal_encoding("utf-8");

// echo 'hey!';

// if (isset($_SESSION['user']['id']))
//     echo "авторизован";
// else
//     echo "не авторизован";

// exit();

$method = $_SERVER['REQUEST_METHOD'];
switch ($method)
{
    case "POST":
        $pars = strpos($_SERVER['REQUEST_URI'], '?');
        $path = explode('/', substr($_SERVER['REQUEST_URI'], 0, ($pars === false) ? null : $pars));
        if ($path[3] === 'user')
        {
            if ($path[4] === 'login')
            {
                if (isset($_SESSION['user']['id']))
                {
                    $data = array(
                        'code' => '201',
                        'message' => 'User already is authorized.',
                        'snippet' => [
                            'userid' => (int)$_SESSION['user']['id'],
                            'useremail' => $_SESSION['user']['email'],
                            'userrole' => $_SESSION['user']['role'],
                        ],
                    );
                }
                else
                {
                    $data = json_decode(file_get_contents('php://input'), true);
                    $email = $data["email"];
                    $pass = $data["pass"];
    
                    require_once '../../service/config.php';
                    $database = 'sgdb';
                    $link = mysqli_connect($host, $user, $password, $database) or die('Потеряно соединение с сервером');
                    
                    $email = htmlentities(mysqli_real_escape_string($link, $email));
    
                    $query = "SELECT `Id`, `Password`, `Role` FROM `users` WHERE `Login` = '$email'";
                    
                    $output = mysqli_query($link, $query) or die('Не удалось загрузить данные.');
                    mysqli_close($link);
    
                    if ($output)
                    {
                        $row = mysqli_fetch_assoc($output);
    
                        $data = null;
                        
                        if ($row && password_verify($pass, $row['Password']) === true)
                        {
                            $_SESSION['user']['id'] = $row['Id'];
                            $_SESSION['user']['email'] = $email;
                            $_SESSION['user']['password'] = $row['Password'];
                            $_SESSION['user']['role'] = $row['Role'];
                            
                            $data = array(
                                'code' => '201',
                                'message' => 'User is authorized.',
                                'snippet' => [
                                    'userid' => (int)$row['Id'],
                                    'useremail' => $email,
                                    'userrole' => $row['Role'],
                                ],
                            );
                        }
                        else
                        {
                            $data['error'] = [
                                'code' => '404',
                                'message' => 'The reqired user is not exist.',
                                'errors' => [
                                    [
                                        'message' => 'Bad password were post.',
                                        'domain' => 'bloggerfans.account',
                                        'reason' => 'userForbidden',
                                    ],
                                ],
                            ];
                        }
                    }
                    else
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'The reqired user is not exist.',
                            'errors' => [
                                [
                                    'message' => 'The reqired user is not exist.',
                                    'domain' => 'bloggerfans.account',
                                    'reason' => 'userNotFound',
                                ],
                            ],
                        ];
                    }
                }
            }
        }

        die (json_encode($data));

        break;
    case 'GET':
        $pars = strpos($_SERVER['REQUEST_URI'], '?');
        $path = explode('/', substr($_SERVER['REQUEST_URI'], 0, ($pars === false) ? null : $pars));

        if ($path[3] === 'user')
        {
            if ($path[4] === 'playlists')
            {
                if (isset($_SESSION['user']['id']))
                {
                    $userId = $_SESSION['user']['id'];
                    
                    $getLimit = 20; // число плейлистов по умолчанию
                    if (isset($_GET['maxResults']))
                    {
                        if ($_GET['maxResults'] > 10)
                            $getLimit = $_GET['maxResults'] > 30 ? 30 : (int)$_GET['maxResults'];
                        else
                            $getLimit = 10; // минимальное число плейлистов
                    }
                    
                    $getShift = $_GET['page'] - 1 ?? 0;

                    try
                    {
                        require_once '../../service/config.php';
                        $database = 'sgdb';

                        $link = mysqli_connect($host, $user, $password, $database) or die('Потеряно соединение с сервером.');
                    
                        $query = "SELECT p.`Id`, p.`Title`, p.`Description`, (SELECT COUNT(*) FROM `compositionsofplaylists` cp WHERE cp.`PlaylistId`=p.`Id`) AS `Summary`, (SELECT a.`PictureName` FROM `albums` a LEFT JOIN `compositions` c ON c.`AlbumId`=a.`Id` LEFT JOIN `compositionsofplaylists` cp ON cp.`CompositionId`=c.`Id` WHERE cp.`PlaylistId`=p.`Id` AND a.`PictureName` IS NOT NULL LIMIT 1) AS `PictureName` FROM `playlists` p WHERE p.`UserId`=$userId LIMIT $getShift, $getLimit;";
                        
                        $result = mysqli_query($link, $query) or die("Ошибка входа в систему.");
                        mysqli_close($link);

                        if ($result)
                        {
                            while($row = mysqli_fetch_assoc($result))
                            {
                                if ($row['PictureName'])
                                {
                                    $picture = file_get_contents('../images/albums/'.$row['PictureName']);
                                    if ($picture)
                                        $picture = base64_encode($picture);
                                    else
                                        $picture = null; // файл не найден
                                }
                                else
                                    $picture = null; // нет альбома/картинки

                                $playlists[] = array(
                                    'kind' => 'music#playlist',
                                    'id' => (int)$row['Id'],
                                    'snippet' => [
                                        'title' => $row['Title'],
                                        'description' => $row['Description'],
                                        'summary' => (int)$row['Summary'],
                                        'picture' => $picture,
                                        // 'pictureName' => $row['PictureName'],
                                    ],
                                );
                            }

                            $data = array(
                                'kind' => 'music#playlistListResponse',
                                'pageInfo' => [
                                    'resultsPerPage' => $getLimit,
                                ],
                                'items' => $playlists,
                            );
                        }
                        else
                        {
                            $data['error'] = [
                                'code' => '404',
                                'message' => 'The reqired playlists are not exist.',
                                'errors' => [
                                    [
                                        'message' => 'The reqired playlists are not exist.',
                                        'domain' => 'bloggerfans.music',
                                        'reason' => 'userPlaylistsNotFound',
                                    ],
                                ],
                            ];
                        }
                    }
                    catch (Exception $e)
                    {
                        $data['error'] = [
                            'code' => '500',
                            'message' => $e->getMessage(),
                            'errors' => [
                                [
                                    'message' => $e->getMessage(),
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'serverError',
                                ],
                            ],
                        ];
                    }
                }
                else
                {
                    $data['error'] = [
                        'code' => '403',
                        'message' => 'The user is unauthorized.',
                        'errors' => [
                            [
                                'message' => 'The user is unauthorized.',
                                'domain' => 'bloggerfans.music',
                                'reason' => 'userPlaylistsForbidden',
                            ],
                        ],
                    ];
                }
            }
        }
        else if ($path[3] === 'playlist')
        {
            if ($path[4] === 'open')
            {
                $getId = (int)$_GET['id'];

                $getLimit = 25; // число композиций по умолчанию
                if (isset($_GET['maxResults']))
                {
                    if ($_GET['maxResults'] > 20)
                        $getLimit = $_GET['maxResults'] > 50 ? 50 : (int)$_GET['maxResults'];
                    else
                        $getLimit = 20; // минимальное число композиций
                }
                    
                $getShift = $_GET['page'] - 1 ?? 0;

                try
                {
                    require_once '../../service/config.php';
                    $database = 'sgdb';

                    $link = mysqli_connect($host, $user, $password, $database) or die('Потеряно соединение с сервером.');
                
                    $query = "SELECT `Title`, `Description`, (SELECT COUNT(*) FROM `compositionsofplaylists` cp WHERE cp.`PlaylistId`=p.`Id`) AS `Total`, (SELECT SUM(`Duration`) FROM `compositions` c LEFT JOIN `compositionsofplaylists` cp ON cp.`CompositionId`=c.`Id` WHERE cp.`PlaylistId`=p.`Id`) AS `Duration` FROM `playlists` p WHERE Id=$getId;";
                    $query .= "SELECT c.`Id`, c.`Name`, c.`Date`, c.`Text`, c.`Auditions`, c.`Explicit`, c.`FileName`, a.`Id` AS `Album Id`, a.`Name` AS `Album Name`, a.`PictureName` FROM `compositions` c LEFT JOIN `compositionsofplaylists` cp ON cp.`CompositionId` = c.`Id` LEFT JOIN `albums` a ON a.`Id` = c.`AlbumId` WHERE cp.`PlaylistId`=$getId LIMIT $getShift, $getLimit;";
                    $query .= "SELECT c.`Id` AS `Composition Id`, a.`Id` AS `Artist Id`, a.`Name` FROM `artists` a LEFT JOIN `compsofartists` ca ON ca.`ArtistId` = a.`Id` LEFT JOIN `compositions` c ON c.Id = ca.`CompositionId` LEFT JOIN `compositionsofplaylists` cp ON cp.`CompositionId` = c.`Id` WHERE cp.`PlaylistId`=$getId LIMIT $getShift, $getLimit;";
                    
                    mysqli_multi_query($link, $query);

                    $result1 = mysqli_store_result($link); // информация о плейлисте

                    mysqli_next_result($link);

                    $result2 = mysqli_store_result($link); // информация о композициях

                    mysqli_next_result($link);

                    $result3 = mysqli_store_result($link); // информация об исполнителях

                    mysqli_close($link);

                    if ($result1 && $result2 && $result3)
                    {
                        $playlist = mysqli_fetch_assoc($result1);

                        $comps = array();

                        $i = 0; $next = true;
                        while($composition = mysqli_fetch_assoc($result2))
                        {
                            $artists = array();

                            for (; $i < mysqli_num_rows($result3);)
                            {
                                if ($next)
                                    $artist = mysqli_fetch_assoc($result3);
                                
                                if ($artist['Composition Id'] === $composition['Id'])
                                {
                                    $artists[] = [
                                        'id' => (int)$artist['Artist Id'],
                                        'name' => $artist['Name'],
                                    ];
                                    $i++;
                                    $next = true;
                                }
                                else
                                {
                                    $next = false;
                                    break;
                                }
                            }

                            
                            if ($composition['PictureName'])
                            {
                                $picture = file_get_contents('../images/albums/'.$composition['PictureName']);
                                if ($picture)
                                    $picture = base64_encode($picture);
                                else
                                    $picture = null; // файл не найден
                            }
                            else
                                $picture = null; // нет альбома/картинки

                            $comps[] = array(
                                'kind' => 'music#composition',
                                'id' => (int)$composition['Id'],
                                'snippet' => [
                                    'name' => $composition['Name'],
                                    'artists' => $artists,
                                    'album' => [
                                        'id' => (int)$composition['Album Id'],
                                        'name' => $composition['Album Name'],
                                        'picture' => $picture,
                                    ],
                                    'date' => $composition['Date'],
                                    'text' => $composition['Text'],
                                    'auditions' => (int)$composition['Auditions'],
                                    'explicit' => (boolean)$composition['Explicit'],
                                    'namepath' => $composition['FileName'],
                                ],
                            );
                        }

                        // $path = 'myfolder/myimage.png';
                        // $type = pathinfo($path, PATHINFO_EXTENSION);
                        // $data = file_get_contents($path);
                        // $base64 = 'data:image/' . $type . ';base64,' . base64_encode($data);

                        $data = array(
                            'kind' => 'music#playlist',
                            'id' => $getId,
                            'duration' => (int)$playlist['Duration'],
                            'snippet' => [
                                'title' => $playlist['Title'],
                                'description' => $playlist['Description'],
                            ],
                            'pageInfo' => [
                                'totalResults' => (int)$playlist['Total'],
                                'resultsPerPage' => $getLimit,
                            ],
                            'items' => $comps,
                        );
                    }
                    else if (!$result1)
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'The reqired playlist is not exist.',
                            'errors' => [
                                [
                                    'message' => 'The reqired playlist is not exist.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'playlistNotFound',
                                ],
                            ],
                        ];
                    }
                    else if (!$result2)
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'Compositions of reqired playlist are not found.',
                            'errors' => [
                                [
                                    'message' => 'Compositions of reqired playlist are not found.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'compositionNotFound',
                                ],
                            ],
                        ];
                    }
                    else
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'Artists of associated compositions are not found.',
                            'errors' => [
                                [
                                    'message' => 'Artists of associated compositions are not found.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'artistNotFound',
                                ],
                            ],
                        ];
                    }
                }
                catch (Exception $e)
                {
                    $data['error'] = [
                        'code' => '500',
                        'message' => $e->getMessage(),
                        'errors' => [
                            [
                                'message' => $e->getMessage(),
                                'domain' => 'bloggerfans.music',
                                'reason' => 'serverError',
                            ],
                        ],
                    ];
                }
            }
        }
        else if ($path[3] === 'sets')
        {
            if (!isset($path[4]))
            {
                $userId = $_SESSION['user']['id'];
                
                $getLimit = 20; // число наборов по умолчанию
                if (isset($_GET['maxResults']))
                {
                    if ($_GET['maxResults'] > 10)
                        $getLimit = $_GET['maxResults'] > 30 ? 30 : (int)$_GET['maxResults'];
                    else
                        $getLimit = 10; // минимальное число наборов
                }
                
                $getShift = $_GET['page'] - 1 ?? 0;

                try
                {
                    require_once '../../service/config.php';
                    $database = 'sgdb';

                    $link = mysqli_connect($host, $user, $password, $database) or die('Потеряно соединение с сервером.');
                
                    $query = "SELECT s.`Id`, s.`Title`, s.`Description`, s.`PictureName`, (SELECT COUNT(*) FROM `compsofeditorschoice` cs WHERE cs.`SetId`=s.`Id`) AS `Summary` FROM `editorschoice` s LIMIT $getShift, $getLimit;";
                    
                    $result = mysqli_query($link, $query) or die("Ошибка входа в систему.");
                    mysqli_close($link);

                    if ($result)
                    {
                        while($row = mysqli_fetch_assoc($result))
                        {
                            $picture = file_get_contents('../images/sets/'.$row['PictureName']);
                            if ($picture)
                                $picture = base64_encode($picture);
                            else
                                $picture = null; // файл не найден

                            $sets[] = array(
                                'kind' => 'music#set',
                                'id' => (int)$row['Id'],
                                'snippet' => [
                                    'title' => $row['Title'],
                                    'description' => $row['Description'],
                                    'summary' => (int)$row['Summary'],
                                    'picture' => $picture,
                                ],
                            );
                        }

                        $data = array(
                            'kind' => 'music#setListResponse',
                            'pageInfo' => [
                                'resultsPerPage' => $getLimit,
                            ],
                            'items' => $sets,
                        );
                    }
                    else
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'The reqired sets are not exist.',
                            'errors' => [
                                [
                                    'message' => 'The reqired sets are not exist.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'setsNotFound',
                                ],
                            ],
                        ];
                    }
                }
                catch (Exception $e)
                {
                    $data['error'] = [
                        'code' => '500',
                        'message' => $e->getMessage(),
                        'errors' => [
                            [
                                'message' => $e->getMessage(),
                                'domain' => 'bloggerfans.music',
                                'reason' => 'serverError',
                            ],
                        ],
                    ];
                }
            }
            else if ($path[4] === 'open')
            {
                // echo "\n".'$path[3] = '.$path[3].'$path[4] = '.$path[4]."\n";

                $getId = (int)$_GET['id'];

                $getLimit = 25; // число композиций по умолчанию
                if (isset($_GET['maxResults']))
                {
                    if ($_GET['maxResults'] > 20)
                        $getLimit = $_GET['maxResults'] > 50 ? 50 : (int)$_GET['maxResults'];
                    else
                        $getLimit = 20; // минимальное число композиций
                }
                    
                $getShift = $_GET['page'] - 1 ?? 0;

                try
                {
                    require_once '../../service/config.php';
                    $database = 'sgdb';

                    $link = mysqli_connect($host, $user, $password, $database) or die('Потеряно соединение с сервером.');
                
                    $query = "SELECT `Title`, `Description`, `PictureName`, (SELECT COUNT(*) FROM `compsofeditorschoice` cs WHERE cs.`SetId`=s.`Id`) AS `Total`, (SELECT SUM(`Duration`) FROM `compositions` c LEFT JOIN `compsofeditorschoice` cs ON cs.`CompositionId`=c.`Id` WHERE cs.`SetId`=s.`Id`) AS `Duration` FROM `editorschoice` s WHERE Id=$getId;";
                    $query .= "SELECT c.`Id`, c.`Name`, c.`Date`, c.`Text`, c.`Auditions`, c.`Explicit`, c.`FileName`, a.`Id` AS `Album Id`, a.`Name` AS `Album Name`, a.`PictureName` FROM `compositions` c LEFT JOIN `compsofeditorschoice` cs ON cs.`CompositionId` = c.`Id` LEFT JOIN `albums` a ON a.`Id` = c.`AlbumId` WHERE cs.`SetId`=$getId LIMIT $getShift, $getLimit;";
                    $query .= "SELECT c.`Id` AS `Composition Id`, a.`Id` AS `Artist Id`, a.`Name` FROM `artists` a LEFT JOIN `compsofartists` ca ON ca.`ArtistId` = a.`Id` LEFT JOIN `compositions` c ON c.Id = ca.`CompositionId` LEFT JOIN `compsofeditorschoice` cs ON cs.`CompositionId` = c.`Id` WHERE cs.`SetId`=$getId LIMIT $getShift, $getLimit;";
                    
                    mysqli_multi_query($link, $query);

                    $result1 = mysqli_store_result($link); // информация о наборе

                    mysqli_next_result($link);

                    $result2 = mysqli_store_result($link); // информация о композициях

                    mysqli_next_result($link);

                    $result3 = mysqli_store_result($link); // информация об исполнителях

                    mysqli_close($link);

                    if ($result1 && $result2 && $result3)
                    {
                        $set = mysqli_fetch_assoc($result1);

                        $comps = array();

                        $i = 0; $next = true;
                        while($composition = mysqli_fetch_assoc($result2))
                        {
                            $artists = array();

                            for (; $i < mysqli_num_rows($result3);)
                            {
                                if ($next)
                                    $artist = mysqli_fetch_assoc($result3);
                                
                                if ($artist['Composition Id'] === $composition['Id'])
                                {
                                    $artists[] = [
                                        'id' => (int)$artist['Artist Id'],
                                        'name' => $artist['Name'],
                                    ];
                                    $i++;
                                    $next = true;
                                }
                                else
                                {
                                    $next = false;
                                    break;
                                }
                            }

                            
                            if ($composition['PictureName'])
                            {
                                $picture = file_get_contents('../images/albums/'.$composition['PictureName']);
                                if ($picture)
                                    $picture = base64_encode($picture);
                                else
                                    $picture = null; // файл не найден
                            }
                            else
                                $picture = null; // нет альбома/картинки

                            $comps[] = array(
                                'kind' => 'music#composition',
                                'id' => (int)$composition['Id'],
                                'snippet' => [
                                    'name' => $composition['Name'],
                                    'artists' => $artists,
                                    'album' => [
                                        'id' => (int)$composition['Album Id'],
                                        'name' => $composition['Album Name'],
                                        'picture' => $picture,
                                    ],
                                    'date' => $composition['Date'],
                                    'text' => $composition['Text'],
                                    'auditions' => (int)$composition['Auditions'],
                                    'explicit' => (boolean)$composition['Explicit'],
                                    'namepath' => $composition['FileName'],
                                ],
                            );
                        }

                        $picture = file_get_contents('../images/sets/'.$set['PictureName']);
                        if ($picture)
                            $picture = base64_encode($picture);
                        else
                            $picture = null; // файл не найден

                        $data = array(
                            'kind' => 'music#set',
                            'id' => $getId,
                            'duration' => (int)$set['Duration'],
                            'snippet' => [
                                'title' => $set['Title'],
                                'description' => $set['Description'],
                                'picture' => $picture,
                            ],
                            'pageInfo' => [
                                'totalResults' => (int)$set['Total'],
                                'resultsPerPage' => $getLimit,
                            ],
                            'items' => $comps,
                        );
                    }
                    else if (!$result1)
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'The reqired set is not exist.',
                            'errors' => [
                                [
                                    'message' => 'The reqired set is not exist.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'setNotFound',
                                ],
                            ],
                        ];
                    }
                    else if (!$result2)
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'Compositions of reqired set are not found.',
                            'errors' => [
                                [
                                    'message' => 'Compositions of reqired set are not found.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'compositionNotFound',
                                ],
                            ],
                        ];
                    }
                    else
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'Artists of associated compositions are not found.',
                            'errors' => [
                                [
                                    'message' => 'Artists of associated compositions are not found.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'artistNotFound',
                                ],
                            ],
                        ];
                    }
                }
                catch (Exception $e)
                {
                    $data['error'] = [
                        'code' => '500',
                        'message' => $e->getMessage(),
                        'errors' => [
                            [
                                'message' => $e->getMessage(),
                                'domain' => 'bloggerfans.music',
                                'reason' => 'serverError',
                            ],
                        ],
                    ];
                }
            }
        }
        else if ($path[3] === 'composition')
        {
            if ($path[4] === 'play')
            {
                $id = $_GET['id'];

                try
                {
                    require_once '../../service/config.php';
                    $database = 'sgdb';

                    $link = mysqli_connect($host, $user, $password, $database) or die('Не удалось загрузить данные.');

                    $query = "SELECT `FileName` FROM `compositions` WHERE `Id` = $id";
                    $result = mysqli_query($link, $query) or die("Ошибка ".mysqli_error($link));
                    mysqli_close($link);

                    if ($result)
                    {
                        $row = mysqli_fetch_assoc($result);
                        
                        $filepath = '../materials/music/'.$row['FileName'];
                        // $filepath = '../materials/Аутро Ильи Exile.mp4';

                        if (!file_exists($filepath))
                        {
                            header("HTTP/1.1 404 Not Found");
                            return;
                        }

                        //на всякий случай очищаю буфер вывода
                        if (ob_get_level())
                        {
                            ob_end_clean();
                        }
                        //header из которого получу имя файла
                        header('Content-Disposition: attachment; filename=' . basename($filepath));

                        //а эти headers нужны в первую очередь для браузера, оставил на всякий случай
                        header('Content-Description: File Transfer');
                        header('Content-Type: application/octet-stream');
                        header('Content-Transfer-Encoding: binary');
                        header('Expires: 0');
                        header('Cache-Control: must-revalidate');
                        header('Pragma: public');
                        header('Content-Length: ' . filesize($filepath));
                        //отдаю файл
                        readfile($filepath);
                        exit;
                    }
                    else
                    {
                        $data['error'] = [
                            'code' => '404',
                            'message' => 'The composition has not found.',
                            'errors' => [
                                [
                                    'message' => 'The composition has not found.',
                                    'domain' => 'bloggerfans.music',
                                    'reason' => 'compositionNotFound',
                                ],
                            ],
                        ];
                    }
                }
                catch (Exception $e)
                {
                    $data['error'] = [
                        'code' => '500',
                        'message' => $e->getMessage(),
                        'errors' => [
                            [
                                'message' => $e->getMessage(),
                                'domain' => 'bloggerfans.music',
                                'reason' => 'serverError',
                            ],
                        ],
                    ];
                }
            }
        }

        die (json_encode($data));
        
        break;
    default:
        die (json_encode([
            'status' => 0,
            'comment' => 'Bad method',
        ]));
        break;
}